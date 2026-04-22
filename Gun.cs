using Godot;

internal sealed partial class Gun : Node3D
{
  [Export] internal int Damage { get; private set; } = 50;

  [Export] private BulletSpawner _bulletSpawner = null!;
  [Export] private PetraCamera _camera = null!;
  [Export] private Node3D _aimPivot = null!;

  [Export] private Node3D _rightLeanPivot = null!;
  [Export] private Node3D _leftLeanPivot = null!;
  private Vector3 _initPos;
  
  [Export] private float _leanLeftAngle = Mathf.Pi / 10f;
  [Export] private float _leanRightAngle = -Mathf.Pi / 10f;
  [Export] private float _leanSpeed = 10f;

  [Export] private Node3D _nearWallState = null!;
  [Export] private RayCast3D _gunRay = null!;

  private Vector3 _rotOffset;
  private Vector3 _swayOffset;
  private Vector3 _wallOffset;

  [ExportGroup("Weapon Sway")]
  [Export] private float _swayAmount = 0.5f;
  [Export] private float _swayThreshold = 0.05f;
  [Export] private float _swayLerpSpeed = 10f;
  [Export] private float _pullBackSpeed = 15f;
  private Vector2 _mouseRelative;

  public override void _Ready()
  {
    _initPos = Position;
    _bulletSpawner.Camera = _camera;
  }

  public override void _UnhandledInput(InputEvent @event)
  {
    if (@event is InputEventMouseMotion mouseMotion)
      _mouseRelative = mouseMotion.Relative;
  }

  public override void _PhysicsProcess(double delta)
  {
    if (Input.IsActionPressed("Aim"))
      Position = Position.Lerp(to: _aimPivot.Position, weight: _leanSpeed * (float)delta);
    else
      HandleLean(delta);
  }

  private void UpdateNearWallOffsets(double delta)
  {
    Vector3 targetPosOffset = Vector3.Zero, targetOrientation = Vector3.Zero;
    float maxDist = _gunRay.TargetPosition.Length(); 

    if (_gunRay.IsColliding())
    {
      float collisionDist = _gunRay.GetCollisionPoint().DistanceTo(_gunRay.GlobalPosition);
      float proximity = Mathf.Clamp(1.0f - (collisionDist / maxDist), 0f, 1f);
      
      targetPosOffset = proximity * (_nearWallState.Position - _initPos);
      targetOrientation = proximity * _nearWallState.Rotation;
    }

    // _gun.Position = _gun.Position.Lerp(to: targetPos, weight: _pullBackSpeed * (float)delta);
    _wallOffset = _wallOffset.Lerp(to: targetPosOffset, weight: _pullBackSpeed * (float)delta);
    // _gun.Rotation = _gun.Rotation.Lerp(to: targetOrientation, weight: _pullBackSpeed * (float)delta);
    _rotOffset = _rotOffset.Lerp(to: targetOrientation, weight: _pullBackSpeed * (float)delta);
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

  private void HandleLean(double delta)
  {
    Vector3 nextPos;
    Quaternion nextRot;
    
    float leanDir = 0f;

    if (Input.IsActionPressed("LeanLeft"))
      leanDir -= 1f;
    if (Input.IsActionPressed("LeanRight"))
      leanDir += 1f;

    if (leanDir != 0f)
    {
      nextPos = leanDir == 1f ? _rightLeanPivot.Position : _leftLeanPivot.Position;
      float leanAngle = leanDir == 1f ? _leanRightAngle : _leanLeftAngle;
      nextRot = Quaternion.FromEuler(new Vector3(0f, 0f, leanAngle));
    }
    else
    {
      nextPos = _initPos;
      nextRot = Quaternion.Identity;
    }

    UpdateNearWallOffsets(delta);
    UpdateSwayOffsets(delta);

    nextPos += _wallOffset + _swayOffset;
    nextRot *= Quaternion.FromEuler(_rotOffset);

    Position = Position.Lerp(to: nextPos, weight: _leanSpeed * (float)delta);
    Quaternion = Quaternion.Slerp(to: nextRot, weight: _leanSpeed * (float)delta);
  }
}
