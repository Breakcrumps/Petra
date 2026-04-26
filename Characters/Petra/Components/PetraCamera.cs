using Godot;
using Petra.Import.Inherited;

namespace Petra.Characters.Petra.Components;

[GlobalClass]
internal sealed partial class PetraCamera : Camera3D
{
  [Export] private PetraChar _petra = null!;
  [Export] private Gun _gun = null!;
  [Export] private ShapeCast3D _leanRaycast = null!;

  [Export] private Node3D _standLeanPivot = null!;
  [Export] private Node3D _crouchPivot = null!;
  [Export] private Node3D _crouchLeanPivot = null!;
  [Export] private Node3D _slideLeanPivot = null!;
  private Vector3 _defaultPos;

  [Export] private float _aimFov = 35f;
  private float _defaultFov;

  [Export] private float _mouseSensitivity = .003f;
  [Export] private float _angleLimit = Mathf.Pi / 3f;

  [Export] private float _leanSpeed = 10f;

  [Export] internal float WalkBobFreq { get; private set; } = 5f;
  [Export] internal float RunBobFreq { get; private set; } = 10f;
  internal float BobFreq { get; set; }
  [Export] private float _bobAmp = .1f;
  private Vector3 _bobOffset;
  private Vector3 _posWithoutBob;

  public override void _Ready()
  {
    Input.MouseMode = Input.MouseModeEnum.Captured;
    _defaultPos = Position;
    _posWithoutBob = Position;
    BobFreq = WalkBobFreq;
    _defaultFov = Fov;
  }

  public override void _UnhandledInput(InputEvent @event)
  {
    if (@event is not InputEventMouseMotion mouseMotion)
      return;

    Vector3 newRotation = Rotation;

    newRotation.X -= mouseMotion.Relative.Y * _mouseSensitivity;
    newRotation.X = Mathf.Clamp(newRotation.X, -_angleLimit, _angleLimit);
    newRotation.Y -= mouseMotion.Relative.X * _mouseSensitivity;

    Rotation = newRotation;
  }

  public override void _PhysicsProcess(double delta)
  {
    switch (_petra.CurrentState)
    {
      case PetraChar.PetraState.Sliding:
        Position = Position.Lerp(to: _slideLeanPivot.Position, weight: 10f * (float)delta);
        return;
      case PetraChar.PetraState.Crouching:
        HandleLeanPos(_crouchPivot.Position, _crouchLeanPivot.Position);
        break;
      default:
        HandleLeanPos(_defaultPos, _standLeanPivot.Position);
        break;
    }

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
      _leanRaycast.TargetPosition = leanPos - _leanRaycast.Position;
      _leanRaycast.ForceShapecastUpdate();
      if (_leanRaycast.IsColliding())
        leanPos = (_leanRaycast.GetCollisionPoint(0) - GlobalPosition) * .95f;
      _posWithoutBob = leanPos with { Y = initY };
    }
    else
    {
      _posWithoutBob = defaultPos;
    }
  }
}
