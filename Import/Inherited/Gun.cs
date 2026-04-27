using Godot;
using Petra.Characters.Petra;
using Petra.Characters.Petra.Components;

namespace Petra.Import.Inherited;

[GlobalClass]
internal sealed partial class Gun : Node3D
{
  [Export] internal int Damage { get; private set; } = 50;
  [Export] private float _delayTime = .075f;
  private float _delayTimer;

  [Export] private BulletSpawner _bulletSpawner = null!;

  [ExportGroup("Needed Nodes")]
  [Export] private PetraChar _petra = null!;
  [Export] private PetraCamera _camera = null!;

  [ExportGroup("Pivots")]
  [Export] private Node3D _rightLeanPivot = null!;
  [Export] private Node3D _leftLeanPivot = null!;
  [Export] private Node3D _aimPivot = null!;
  [Export] private Node3D _rightLeanAimPivot = null!;
  [Export] private Node3D _leftLeanAimPivot = null!;
  [Export] private Node3D _crouchPivot = null!;
  [Export] private Node3D _crouchRightLeanPivot = null!;
  [Export] private Node3D _crouchLeftLeanPivot = null!;
  [Export] private Node3D _runPivot = null!;
  [Export] private Node3D _backRunPivot = null!;
  [Export] private Node3D _heapAimPivot = null!;
  [Export] private Node3D _slidePivot = null!;
  private Vector3 _initPos;
  
  [ExportGroup("Lean Orientation")]
  [Export] private float _leanLeftAngle = Mathf.Pi / 10f;
  [Export] private float _leanRightAngle = -Mathf.Pi / 10f;
  [Export] private float _leanSpeed = 10f;

  [Export] private Node3D _nearWallState = null!;
  [Export] private RayCast3D _gunRay = null!;

  private Vector3 _rotOffset;
  private Vector3 _swayOffset;
  private Vector3 _wallOffset;
  private Vector3 _bobOffset;
  private Vector3 _bobRotOffset;
  private Vector3 _recoilPosOffset;
  private Vector3 _recoilRotOffset;
  private Vector3 _jumpRotOffset;

  [ExportGroup("Weapon Sway")]
  [Export] private float _swayAmount = .5f;
  [Export] private float _swayThreshold = .05f;
  [Export] private float _swayLerpSpeed = 10f;
  [Export] private float _pullBackSpeed = 25f;
  [Export] private float _bobAmp = .05f;
  [Export] private float _bobRotAmp = .08f;
  [Export] private float _leftRightAmp = .05f;
  [Export] private float _returnToPosSpeed = 15f;
  [Export] private float _aimSpeed = 20f;
  [Export] private Vector3 _recoilPosOffsetTarget = new(0f, .1f, .3f);
  [Export] private Vector3 _recoilRotOffsetTarget = new(Mathf.Pi / 8f, 0f, 0f);
  [Export] private Vector3 _aimRecoilPosOffsetTarget = new(0f, .05f, .1f);
  [Export] private Vector3 _aimRecoilRotOffsetTarget = new(Mathf.Pi / 100f, 0f, 0f);
  private Vector2 _mouseRelative;

  internal bool InAim { get; private set; }

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
    if (_petra.CurrentState == PetraChar.PetraState.Sliding)
    {
      Position = Position.Lerp(to: _slidePivot.Position, weight: 10f * (float)delta);
      return;
    }
    
    _camera.BobFreq = _petra.CurrentState == PetraChar.PetraState.Running ? _camera.RunBobFreq : _camera.WalkBobFreq;
    
    HandleFire(delta);
    UpdateSwayOffsets(delta);
    UpdateBobOffsets(delta);
    UpdateJumpOffsets(delta);

    InAim = false;

    if (Input.IsActionPressed("Aim"))
      HandleAimPos(delta);
    else
      HandlePos(delta);
  }

  private void HandleAimPos(double delta)
  {
    Vector3 nextPos;
    Quaternion nextOrient;
    
    float leanDir = 0f;

    if (Input.IsActionPressed("LeanLeft"))
      leanDir -= 1f;
    if (Input.IsActionPressed("LeanRight"))
      leanDir += 1f;

    if (leanDir != 0f)
    {
      if (_gunRay.IsColliding() || _petra.CurrentState == PetraChar.PetraState.Running)
      {
        nextPos = _heapAimPivot.Position;
      }
      else
      {
        nextPos = leanDir > 0f ? _rightLeanAimPivot.Position : _leftLeanAimPivot.Position;
        InAim = true;
      }
      float leanAngle = leanDir == 1f ? _leanRightAngle : _leanLeftAngle;
      nextOrient = Quaternion.FromEuler(new Vector3(0f, 0f, leanAngle));
    }
    else
    {
      if (_gunRay.IsColliding() || _petra.CurrentState == PetraChar.PetraState.Running)
      {
        nextPos = _heapAimPivot.Position;
        nextOrient = _heapAimPivot.Quaternion;
      }
      else
      {
        InAim = true;
        nextPos = _aimPivot.Position;
        nextOrient = Quaternion.Identity;
      }
    }

    nextPos += _bobOffset + _swayOffset + _recoilPosOffset;
    nextOrient *= Quaternion.FromEuler(_recoilRotOffset) * Quaternion.FromEuler(_jumpRotOffset);
    
    Position = Position.Lerp(to: nextPos, weight: _aimSpeed * (float)delta);
    Quaternion = Quaternion.Slerp(to: nextOrient, weight: 10f * (float)delta);
  }

  private void HandlePos(double delta)
  {
    Vector3 nextPos;
    Quaternion nextRot;
    
    float leanDir = 0f;

    if (Input.IsActionPressed("LeanLeft"))
      leanDir -= 1f;
    if (Input.IsActionPressed("LeanRight"))
      leanDir += 1f;

    Vector3 defaultPos, rightLeanPos, leftLeanPos;

    if (_petra.CurrentState == PetraChar.PetraState.Crouching)
    {
      defaultPos = _crouchPivot.Position;
      rightLeanPos = _crouchRightLeanPivot.Position;
      leftLeanPos = _crouchLeftLeanPivot.Position;
    }
    else
    {
      defaultPos = _initPos;
      rightLeanPos = _rightLeanPivot.Position;
      leftLeanPos = _leftLeanPivot.Position;
    }

    if (leanDir != 0f)
    {
      nextPos = leanDir > 0f ? rightLeanPos : leftLeanPos;
      float leanAngle = leanDir > 0f ? _leanRightAngle : _leanLeftAngle;
      nextRot = Quaternion.FromEuler(new Vector3(0f, 0f, leanAngle));
    }
    else
    {
      if (_petra.CurrentState == PetraChar.PetraState.Running)
      {
        nextPos = Input.IsActionPressed("Down") ? _backRunPivot.Position : _runPivot.Position;
        nextRot = Input.IsActionPressed("Down") ? _backRunPivot.Quaternion : _runPivot.Quaternion;
      }
      else
      {
        nextPos = defaultPos;
        nextRot = Quaternion.Identity;
      }
    }

    UpdateNearWallOffsets(delta);

    nextPos += _wallOffset + _swayOffset + _bobOffset + _recoilPosOffset;
    nextRot *= (
      Quaternion.FromEuler(_rotOffset)
      * Quaternion.FromEuler(_bobRotOffset)
      * Quaternion.FromEuler(_recoilRotOffset)
      * Quaternion.FromEuler(_jumpRotOffset)
    );

    Position = Position.Lerp(to: nextPos, weight: _leanSpeed * (float)delta);
    Quaternion = Quaternion.Slerp(to: nextRot, weight: _leanSpeed * (float)delta);
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

    _wallOffset = _wallOffset.Lerp(to: targetPosOffset, weight: _pullBackSpeed * (float)delta);
    _rotOffset = _rotOffset.Lerp(to: targetOrientation, weight: _pullBackSpeed * (float)delta);
  }

  private void UpdateSwayOffsets(double delta)
  {
    if (_mouseRelative.LengthSquared() < .5f)
      _mouseRelative = Vector2.Zero;
    else if ((_mouseRelative.X > 0 && _swayOffset.X > 0) || (_mouseRelative.X < 0 && _swayOffset.X < 0))
      _mouseRelative.X = 0;
    
    float targetSwayX = -_mouseRelative.X * _swayAmount;
    float targetSwayY = _mouseRelative.Y * _swayAmount;

    targetSwayX = Mathf.Clamp(targetSwayX, -_swayThreshold, _swayThreshold);
    targetSwayY = Mathf.Clamp(targetSwayY, -_swayThreshold, _swayThreshold);

    Vector3 targetSway = new(targetSwayX, targetSwayY, 0);

    _swayOffset = _swayOffset.Lerp(targetSway, _swayLerpSpeed * (float)delta);

    _mouseRelative *= .2f;
  }

  private void UpdateBobOffsets(double delta)
  {
    if (_petra.TimeMoving != 0f)
    {
      _bobOffset.Y = -_bobAmp * Mathf.Abs(Mathf.Sin(_camera.BobFreq * _petra.TimeMoving));
      _bobOffset.X = -_leftRightAmp * Mathf.Abs(Mathf.PosMod(_petra.TimeMoving - Mathf.Pi / _camera.BobFreq,  2f * Mathf.Pi / _camera.BobFreq) - Mathf.Pi / _camera.BobFreq);
      _bobRotOffset.X = _bobRotAmp * Mathf.Abs(Mathf.Sin(_camera.BobFreq * (_petra.TimeMoving + .01f * _camera.BobFreq)));
    }
    else
    {
      _bobOffset = _bobOffset.Lerp(to: Vector3.Zero, weight: _returnToPosSpeed * (float)delta);
      _bobRotOffset = _bobRotOffset.Lerp(to: Vector3.Zero, weight: _returnToPosSpeed * (float)delta);
    }
  }

  private void UpdateJumpOffsets(double delta)
  {
    if (_petra.Velocity.Y != 0f)
    {
      float targetRotOffset = -Mathf.Clamp(_petra.Velocity.Y, -10f, 10f) * Mathf.Pi / 200f;
      _jumpRotOffset = _jumpRotOffset.Lerp(to: _jumpRotOffset with { X = targetRotOffset }, weight: 50f * (float)delta);
    }
    else
    {
      _jumpRotOffset = _jumpRotOffset.Lerp(to: Vector3.Zero, weight: 10f * (float)delta);
    }
  }

  private void HandleFire(double delta)
  {
    if (
      Input.IsActionJustPressed("Fire")
      && (Input.IsActionPressed("Aim")
        || !_gunRay.IsColliding()
        && _petra.CurrentState != PetraChar.PetraState.Running
      )
      && _delayTimer == 0f
    )
    {
      _bulletSpawner.Fire();
      _recoilPosOffset = Input.IsActionPressed("Aim") ? _aimRecoilPosOffsetTarget : _recoilPosOffsetTarget;
      _recoilRotOffset = Input.IsActionPressed("Aim") ? _aimRecoilRotOffsetTarget : _recoilRotOffsetTarget;
      _recoilPosOffset.X = Input.IsActionPressed("Aim") ? (GD.Randf() - .5f) / 12f : (GD.Randf() - .5f) / 4f;
      _recoilRotOffset.Y = Input.IsActionPressed("Aim") ? (GD.Randf() - .5f) / 12f : (GD.Randf() - .5f) / 4f;
      if (_petra.CurrentState == PetraChar.PetraState.Crouching)
      {
        _recoilPosOffset.Y *= .5f;
        _recoilRotOffset.X *= .5f;
      }
      _delayTimer = _delayTime;
    }
    else
    {
      _recoilPosOffset = _recoilPosOffset.Lerp(to: Vector3.Zero, weight: 20f * (float)delta);
      _recoilRotOffset = _recoilRotOffset.Lerp(to: Vector3.Zero, weight: 20f * (float)delta);
      _delayTimer = Mathf.Max(_delayTimer - (float)delta, 0f);
    }
  }
}
