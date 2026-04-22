using Godot;

[GlobalClass]
internal sealed partial class PetraCamera : Camera3D
{
  [Export] private Petra _petra = null!;

  [Export] private Node3D _nearWallState = null!;
  [Export] private RayCast3D _gunRay = null!;
  [Export] private Gun _gun = null!;
  private Vector3 _gunDefaultPos;

  [Export] private Node3D _standLeanPivot = null!;
  [Export] private Node3D _crouchPivot = null!;
  [Export] private Node3D _crouchLeanPivot = null!;
  private Vector3 _defaultPos;

  [Export] private float _mouseSensitivity = .003f;
  [Export] private float _angleLimit = Mathf.Pi / 3f;

  [Export] private float _leanSpeed = 10f;
  private float _pullBackSpeed = 15f;

  [ExportGroup("Weapon Sway")]
  [Export] private float _swayAmount = 0.5f;
  [Export] private float _swayThreshold = 0.05f;
  [Export] private float _swayLerpSpeed = 10f;
  private Vector2 _mouseRelative;

  private Vector3 _swayOffset;
  private Vector3 _wallOffset;

  public override void _Ready()
  {
    Input.MouseMode = Input.MouseModeEnum.Captured;
    _defaultPos = Position;
    _gunDefaultPos = _gun.Position;
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

    _mouseRelative = mouseMotion.Relative;
  }

  public override void _PhysicsProcess(double delta)
  {
    switch (_petra.CurrentState)
    {
      case Petra.PetraState.Crouching:
        HandleLeanPos(_crouchPivot.Position, _crouchLeanPivot.Position, delta);
        break;
      default:
        HandleLeanPos(_defaultPos, _standLeanPivot.Position, delta);
        break;
    }

    UpdateNearWallOffsets(delta);
    UpdateSwayOffsets(delta);

    _gun.ExtPosOffset = _swayOffset + _wallOffset;
  }

  private void HandleLeanPos(Vector3 defaultPos, Vector3 leanPos, double delta)
  {
    float leanDir = 0f;

    if (Input.IsActionPressed("LeanLeft"))
      leanDir -= 1f;
    if (Input.IsActionPressed("LeanRight"))
      leanDir += 1f;

    if (leanDir != 0f)
    {
      Vector3 targetPos = leanPos;
      targetPos.X *= leanDir;
      targetPos = targetPos.Rotated(Vector3.Up, Rotation.Y);
      Position = Position.Lerp(to: targetPos, weight: _leanSpeed * (float)delta);
    }
    else
    {
      Position = Position.Lerp(to: defaultPos, weight: _leanSpeed * (float)delta);
    }
  }

  private void UpdateNearWallOffsets(double delta)
  {
    Vector3 targetPosOffset = Vector3.Zero, targetOrientation = Vector3.Zero;
    float maxDist = _gunRay.TargetPosition.Length(); 

    if (_gunRay.IsColliding())
    {
      float collisionDist = _gunRay.GetCollisionPoint().DistanceTo(_gunRay.GlobalPosition);
      float proximity = Mathf.Clamp(1.0f - (collisionDist / maxDist), 0f, 1f);
      
      targetPosOffset = proximity * (_nearWallState.Position - _gunDefaultPos);
      targetOrientation = proximity * _nearWallState.Rotation;
    }

    // _gun.Position = _gun.Position.Lerp(to: targetPos, weight: _pullBackSpeed * (float)delta);
    _wallOffset = _wallOffset.Lerp(to: targetPosOffset, weight: _pullBackSpeed * (float)delta);
    // _gun.Rotation = _gun.Rotation.Lerp(to: targetOrientation, weight: _pullBackSpeed * (float)delta);
    _gun.ExtRotOffset = _gun.ExtRotOffset.Lerp(to: targetOrientation, weight: _pullBackSpeed * (float)delta);
  }

  private void UpdateSwayOffsets(double delta)
  {
    if (_mouseRelative.LengthSquared() < .5f)
    {
      _mouseRelative = Vector2.Zero;
      return;
    }
    else if ((_mouseRelative.X > 0 && _swayOffset.X > 0) || (_mouseRelative.X < 0 && _swayOffset.X < 0))
      _mouseRelative.X = 0;
    
    float targetSwayX = -_mouseRelative.X * _swayAmount;
    float targetSwayY = _mouseRelative.Y * _swayAmount;

    targetSwayX = Mathf.Clamp(targetSwayX, -_swayThreshold, _swayThreshold);
    targetSwayY = Mathf.Clamp(targetSwayY, -_swayThreshold, _swayThreshold);

    Vector3 targetSway = new(targetSwayX, targetSwayY, 0);

    // _gun.Position = _gun.Position.Lerp(_gunDefaultPos + swayPos, _swayLerpSpeed * (float)delta);
    _swayOffset = _swayOffset.Lerp(targetSway, _swayLerpSpeed * (float)delta);

    _mouseRelative *= .2f;
  }
}
