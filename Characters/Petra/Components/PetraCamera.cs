using Godot;
using Petra.Objects.Guns.Scripts;

namespace Petra.Characters.Petra.Components;

[GlobalClass]
internal sealed partial class PetraCamera : Camera3D
{
  [Export] private PetraChar _petra = null!;
  [Export] internal Camera3D GunCamera = null!;
  [Export] private GunsWrapper _gun = null!;
  [Export] private ShapeCast3D _leanRaycast = null!;

  [Export] private Marker3D _standLeanPivot = null!;
  [Export] private Marker3D _crouchPivot = null!;
  [Export] private Marker3D _crouchLeanPivot = null!;
  [Export] private Marker3D _slidePivot = null!;
  [Export] private Marker3D _climbMarker = null!;

  private Vector3 _defaultPos;

  [Export] private float _aimFov = 50f;
  private float _defaultFov;

  [Export] private float _mouseSensitivity = .003f;
  [Export] private float _angleLimit = Mathf.Pi / 2.5f;

  [Export] private float _leanSpeed = 10f;

  [Export] internal float WalkBobFreq = 5f;
  [Export] internal float RunBobFreq = 9f;
  internal float BobFreq;
  [Export] private float _bobAmp = .1f;
  private Vector3 _bobOffset;
  private Vector3 _posWithoutBob;

  private Vector3 _nextRot;

  internal void Die()
  {
    _petra.GlobalBasis = Basis.LookingAt(-GlobalBasis.Z with { Y = 0f });
    Rotation = Vector3.Zero;
    SetProcessInput(false);

    float fallSide = GD.Randf() > .5f ? 1f : -1f;
    float kneelHeight = Position.Y * .3f;

    Tween fovTween = CreateTween();
    fovTween.TweenProperty(this, "fov", Fov - 5f, 2.2)
      .SetTrans(Tween.TransitionType.Linear)
      .SetEase(Tween.EaseType.In);

    Tween tween = CreateTween();
    tween.SetParallel(true);

    tween.TweenProperty(this, "position:y", kneelHeight, .4)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.In);

    tween.TweenProperty(this, "rotation_degrees:x", -25f, .4)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.In);

    tween.TweenProperty(this, "rotation_degrees:z", 12f * fallSide, .4)
      .SetTrans(Tween.TransitionType.Sine)
      .SetEase(Tween.EaseType.InOut);

    tween.TweenInterval(.2);

    tween.Chain().TweenProperty(this, "position:y", kneelHeight + .08f, .9)
      .SetTrans(Tween.TransitionType.Linear);
    tween.TweenProperty(this, "rotation_degrees:z", 10f * fallSide, .9);

    tween.Chain().TweenProperty(this, "position:y", .1f, .7)
      .SetTrans(Tween.TransitionType.Bounce)
      .SetEase(Tween.EaseType.Out);

    tween.TweenProperty(this, "rotation_degrees:z", 80f * fallSide, .55)
      .SetTrans(Tween.TransitionType.Cubic)
      .SetEase(Tween.EaseType.In);

    tween.TweenProperty(this, "rotation_degrees:x", 5f, .55)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.In);

    tween.Chain().TweenCallback(Callable.From(() => _petra.Fallen?.Invoke()));
  }

  public override void _Ready()
  {
    Input.MouseMode = Input.MouseModeEnum.Captured;
    _defaultPos = Position;
    _posWithoutBob = Position;
    BobFreq = WalkBobFreq;
    _defaultFov = Fov;
    _nextRot = Rotation;
    _leanRaycast.Enabled = false;
  }

  public override void _UnhandledInput(InputEvent @event)
  {
    if (@event is not InputEventMouseMotion mouseMotion)
      return;

    _nextRot.X -= mouseMotion.Relative.Y * _mouseSensitivity;
    _nextRot.X = Mathf.Clamp(_nextRot.X, -_angleLimit, _angleLimit);
    _nextRot.Y -= mouseMotion.Relative.X * _mouseSensitivity;
  }

  public override void _PhysicsProcess(double delta)
  {
    if (_petra.CurrentState is PetraChar.PetraState.Dead)
      return;
    
    Rotation = _nextRot;

    switch (_petra.CurrentState)
    {
      case PetraChar.PetraState.Sliding:
        Position = Position.Lerp(to: _slidePivot.Position, weight: 10f * (float)delta);
        break;
      case PetraChar.PetraState.Crouching:
        HandleLeanPos(_petra.IsOnFloor() ? _crouchPivot.Position : _defaultPos, _petra.IsOnFloor() ? _crouchLeanPivot.Position : _standLeanPivot.Position);
        ApplyFrame(delta);
        break;
      default:
        HandleLeanPos(_defaultPos, _standLeanPivot.Position);
        ApplyFrame(delta);
        break;
    }

    GunCamera.GlobalPosition = GlobalPosition;
    GunCamera.Rotation = Rotation;
  }

  private void ApplyFrame(double delta)
  {
    if (_petra.TimeMoving != 0f)
      _bobOffset.Y = _bobAmp * Mathf.Abs(Mathf.Sin(BobFreq * _petra.TimeMoving));
    else
      _bobOffset = _bobOffset.Lerp(to: Vector3.Zero, weight: 10f * (float)delta);

    Position = Position.Lerp(_posWithoutBob + _bobOffset, 10f * (float)delta);
    Fov = Mathf.Lerp(from: Fov, to: _gun.InAim ? _aimFov : _defaultFov, weight: 15f * (float)delta);
  }

  private void HandleLeanPos(Vector3 defaultPos, Vector3 leanPos)
  {
    float leanDir = 0f;

    if (Input.IsActionPressed("LeanLeft"))
      leanDir -= 1f;
    if (Input.IsActionPressed("LeanRight"))
      leanDir += 1f;

    if (leanDir != 0f)
    {
      float initY = leanPos.Y;
      leanPos.X *= leanDir;
      leanPos = leanPos.Rotated(Vector3.Up, Rotation.Y);
      _leanRaycast.Enabled = true;
      _leanRaycast.TargetPosition = leanPos - _leanRaycast.Position;
      _leanRaycast.ForceShapecastUpdate();
      if (_leanRaycast.IsColliding())
        leanPos = (_leanRaycast.GetCollisionPoint(0) - GlobalPosition) * .95f;
      _leanRaycast.Enabled = false;
      _posWithoutBob = leanPos with { Y = initY };
    }
    else
    {
      _posWithoutBob = defaultPos;
    }
  }
}
