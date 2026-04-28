using Godot;
using Petra.Characters.Petra;
using Petra.Characters.Petra.Components;
using Petra.Guns;

namespace Petra.Resources.Objects.Guns;

[GlobalClass]
internal sealed partial class Gun : Node3D
{
  [Export] internal GunData GunData { get; private set; } = null!;
  [Export] internal BulletSpawner BulletSpawner { get; private set; } = null!;
  private float _delayTimer;

  [Export] private PetraChar _petra = null!;
  [Export] private PetraCamera _camera = null!;
  [Export] private RayCast3D _gunRay = null!;
  [Export] private PackedScene _smokeScene = null!;
  [Export] private PackedScene _sparksScene = null!;
  [Export] private int _poolSize = 10;
  private GpuParticles3D[] _smokePool = null!;
  private DelayedParticles[] _sparksPool = null!;
  private int _nextParticleIdx = 0;

  [Export] private Texture2D _muzzleFlashImage = null!;
  private Sprite3D _muzzleFlashSprite = null!;

  private OmniLight3D[] _localMuzzleFlashes = null!;
  [Export] private OmniLight3D _muzzleFlash = null!;
  private float _muzzleTimer;
  private float _defaultMuzzleEnergy;

  private MeshInstance3D _meshNode = null!;

  private PosAndRot _swayOffset = new();
  private PosAndRot _nearWallOffset = new();
  private PosAndRot _bobOffset = new();
  private PosAndRot _recoilOffset = new();
  private PosAndRot _jumpOffset = new();
  private Vector2 _mouseRelative;

  internal bool InAim { get; private set; }

  public override void _Ready()
  {
    BulletSpawner.Position = GunData.BulletSpawnerPos;
    BulletSpawner.Camera = _camera;

    _localMuzzleFlashes = new OmniLight3D[2];
    CreateLocalMuzzleFlash(0, -Basis.Z + Basis.Y);
    CreateLocalMuzzleFlash(1, -Basis.Z - Basis.X);

    _defaultMuzzleEnergy = _muzzleFlash.LightEnergy;
    _muzzleFlash.LightEnergy = 0f;
    _muzzleFlash.Position = BulletSpawner.Position;

    _smokePool = new GpuParticles3D[_poolSize];

    for (int i = 0; i < _poolSize; i++)
    {
      GpuParticles3D newSmoke = _smokeScene.Instantiate<GpuParticles3D>();
      GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, newSmoke);
      _smokePool[i] = newSmoke;
    }

    _sparksPool = new DelayedParticles[_poolSize];

    for (int i = 0; i < _poolSize; i++)
    {
      DelayedParticles newSparks = _sparksScene.Instantiate<DelayedParticles>();
      GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, newSparks);
      _sparksPool[i] = newSparks;
    }

    _muzzleFlashSprite = new();
    BulletSpawner.AddChild(_muzzleFlashSprite);
    _muzzleFlashSprite.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
    _muzzleFlashSprite.Transparency = 1f;
    _muzzleFlashSprite.Layers = 2;
    _muzzleFlashSprite.Texture = _muzzleFlashImage;
    _muzzleFlashSprite.Scale *= .25f;

    _meshNode = new();
    AddChild(_meshNode);
    _meshNode.Layers = 2;
    _meshNode.Mesh = GunData.Mesh;
  }

  private void CreateLocalMuzzleFlash(int idx, Vector3 offset)
  {
    BulletSpawner.AddChild(_localMuzzleFlashes[idx] = new());
    _localMuzzleFlashes[idx].LightEnergy = 0f;
    _localMuzzleFlashes[idx].Position = offset;
    _localMuzzleFlashes[idx].LightColor = GunData.MuzzleFlashColor;
    _localMuzzleFlashes[idx].Layers = 2;
  }

  internal void LoadData(GunData data)
  {
    GunData = data;
    BulletSpawner.Position = GunData.BulletSpawnerPos;
    _meshNode.Mesh = GunData.Mesh;
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
      Position = Position.Lerp(to: GunData.SlidePos, weight: 10f * (float)delta);
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
        nextPos = GunData.HeapAimPos;
      }
      else
      {
        nextPos = leanDir > 0f ? GunData.RightLeanAimPos : GunData.LeftLeanAimPos;
        InAim = true;
      }
      float leanAngle = leanDir == 1f ? GunData.LeanRightAngle : GunData.LeanLeftAngle;
      nextOrient = Quaternion.FromEuler(new Vector3(0f, 0f, leanAngle));
    }
    else
    {
      if (_gunRay.IsColliding() || _petra.CurrentState == PetraChar.PetraState.Running)
      {
        nextPos = GunData.HeapAimPos;
        nextOrient = GunData.HeapAimOrient;
      }
      else
      {
        InAim = true;
        nextPos = GunData.AimPos;
        nextOrient = Quaternion.Identity;
      }
    }

    nextPos += _bobOffset.Position + _swayOffset.Position + _recoilOffset.Position;
    nextOrient *= Quaternion.FromEuler(_recoilOffset.Rotation) * Quaternion.FromEuler(_jumpOffset.Rotation);
    
    Position = Position.Lerp(to: nextPos, weight: GunData.AimSpeed * (float)delta);
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
      defaultPos = GunData.CrouchPos;
      rightLeanPos = GunData.CrouchRightLeanPos;
      leftLeanPos = GunData.CrouchLeftLeanPos;
    }
    else
    {
      defaultPos = GunData.DefaultPos;
      rightLeanPos = GunData.RightLeanPos;
      leftLeanPos = GunData.LeftLeanPos;
    }

    if (leanDir != 0f)
    {
      nextPos = leanDir > 0f ? rightLeanPos : leftLeanPos;
      float leanAngle = leanDir > 0f ? GunData.LeanRightAngle : GunData.LeanLeftAngle;
      nextRot = Quaternion.FromEuler(new Vector3(0f, 0f, leanAngle));
    }
    else
    {
      if (_petra.CurrentState == PetraChar.PetraState.Running)
      {
        nextPos = Input.IsActionPressed("Down") ? GunData.BackRunPos : GunData.RunPos;
        nextRot = Input.IsActionPressed("Down") ? GunData.BackRunOrient: GunData.RunOrient;
      }
      else
      {
        nextPos = defaultPos;
        nextRot = Quaternion.Identity;
      }
    }

    UpdateNearWallOffsets(delta);

    nextPos += _nearWallOffset.Position + _swayOffset.Position + _bobOffset.Position + _recoilOffset.Position;
    nextRot *= (
      Quaternion.FromEuler(_nearWallOffset.Rotation)
      * Quaternion.FromEuler(_bobOffset.Rotation)
      * Quaternion.FromEuler(_recoilOffset.Rotation)
      * Quaternion.FromEuler(_jumpOffset.Rotation)
    );

    Position = Position.Lerp(to: nextPos, weight: GunData.LeanSpeed * (float)delta);
    Quaternion = Quaternion.Slerp(to: nextRot, weight: GunData.LeanSpeed * (float)delta);
  }

  private void UpdateNearWallOffsets(double delta)
  {
    Vector3 targetPosOffset = Vector3.Zero, targetOrientation = Vector3.Zero;
    float maxDist = _gunRay.TargetPosition.Length(); 

    if (_gunRay.IsColliding())
    {
      float collisionDist = _gunRay.GetCollisionPoint().DistanceTo(_gunRay.GlobalPosition);
      float proximity = Mathf.Clamp(1.0f - (collisionDist / maxDist), 0f, 1f);
      
      targetPosOffset = proximity * (GunData.NearWallPos - GunData.DefaultPos);
      targetOrientation = proximity * GunData.NearWallRot;
    }

    _nearWallOffset.Position = _nearWallOffset.Position.Lerp(to: targetPosOffset, weight: GunData.PullBackSpeed * (float)delta);
    _nearWallOffset.Rotation = _nearWallOffset.Rotation.Lerp(to: targetOrientation, weight: GunData.PullBackSpeed * (float)delta);
  }

  private void UpdateSwayOffsets(double delta)
  {
    if (_mouseRelative.LengthSquared() < .5f)
      _mouseRelative = Vector2.Zero;
    else if ((_mouseRelative.X > 0 && _swayOffset.Position.X > 0) || (_mouseRelative.X < 0 && _swayOffset.Position.X < 0))
      _mouseRelative.X = 0;
    
    float targetSwayX = -_mouseRelative.X * GunData.SwayAmount;
    float targetSwayY = _mouseRelative.Y * GunData.SwayAmount;

    targetSwayX = Mathf.Clamp(targetSwayX, -GunData.SwayThreshold, GunData.SwayThreshold);
    targetSwayY = Mathf.Clamp(targetSwayY, -GunData.SwayThreshold, GunData.SwayThreshold);

    Vector3 targetSway = new(targetSwayX, targetSwayY, 0);

    _swayOffset.Position = _swayOffset.Position.Lerp(targetSway, GunData.SwayLerpSpeed * (float)delta);

    _mouseRelative *= .2f;
  }

  private void UpdateBobOffsets(double delta)
  {
    if (_petra.TimeMoving != 0f)
    {
      _bobOffset.Position.Y = -GunData.BobAmp * Mathf.Abs(Mathf.Sin(_camera.BobFreq * _petra.TimeMoving));
      _bobOffset.Position.X = -GunData.LeftRightAmp * Mathf.Abs(Mathf.PosMod(_petra.TimeMoving - Mathf.Pi / _camera.BobFreq,  2f * Mathf.Pi / _camera.BobFreq) - Mathf.Pi / _camera.BobFreq);
      _bobOffset.Rotation.X = GunData.BobRotAmp * Mathf.Abs(Mathf.Sin(_camera.BobFreq * (_petra.TimeMoving + .01f * _camera.BobFreq)));
    }
    else
    {
      _bobOffset.Position = _bobOffset.Position.Lerp(to: Vector3.Zero, weight: GunData.ReturnToPosSpeed * (float)delta);
      _bobOffset.Rotation = _bobOffset.Rotation.Lerp(to: Vector3.Zero, weight: GunData.ReturnToPosSpeed * (float)delta);
    }
  }

  private void UpdateJumpOffsets(double delta)
  {
    if (_petra.Velocity.Y != 0f)
    {
      float targetRotOffset = -Mathf.Clamp(_petra.Velocity.Y, -10f, 10f) * Mathf.Pi / 200f;
      _jumpOffset.Rotation = _jumpOffset.Rotation.Lerp(
        to: _jumpOffset.Rotation with { X = targetRotOffset },
        weight: 50f * (float)delta
      );
    }
    else
    {
      _jumpOffset.Rotation = _jumpOffset.Rotation.Lerp(to: Vector3.Zero, weight: 10f * (float)delta);
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
      BulletSpawner.Fire();
      _recoilOffset.Position = Input.IsActionPressed("Aim") ? GunData.AimRecoilOffsetTarget.Position : GunData.RecoilOffsetTarget.Position;
      _recoilOffset.Rotation = Input.IsActionPressed("Aim") ? GunData.AimRecoilOffsetTarget.Rotation : GunData.RecoilOffsetTarget.Rotation;
      _recoilOffset.Position.X = Input.IsActionPressed("Aim") ? (GD.Randf() - .5f) / 12f : (GD.Randf() - .5f) / 4f;
      _recoilOffset.Rotation.Y = Input.IsActionPressed("Aim") ? (GD.Randf() - .5f) / 12f : (GD.Randf() - .5f) / 4f;
      if (_petra.CurrentState == PetraChar.PetraState.Crouching)
      {
        _recoilOffset.Position.Y *= .5f;
        _recoilOffset.Rotation.X *= .5f;
      }
      _delayTimer = GunData.DelayTime;
      // _localMuzzleFlash.Visible = true;
      // _muzzleFlash.Visible = true;
      _muzzleTimer = GunData.MuzzleTime;
      _muzzleFlash.LightEnergy = _defaultMuzzleEnergy;
      for (int i = 0; i < _localMuzzleFlashes.Length; i++)
        _localMuzzleFlashes[i].LightEnergy = GunData.LocalMuzzleFlashEnergy;
      _muzzleFlashSprite.Visible = true;

      GpuParticles3D nextSmoke = _smokePool[_nextParticleIdx];
      nextSmoke.GlobalPosition = _camera.GlobalPosition + BulletSpawner.GlobalPosition;
      nextSmoke.Transform = nextSmoke.Transform.LookingAt(nextSmoke.GlobalPosition - BulletSpawner.GlobalBasis.Z);
      nextSmoke.Restart();
      nextSmoke.AmountRatio = GD.Randf();
      nextSmoke.Emitting = true;
      DelayedParticles nextSparks = _sparksPool[_nextParticleIdx];
      nextSparks.GlobalPosition = _camera.GlobalPosition + BulletSpawner.GlobalPosition;
      nextSparks.Transform = nextSparks.Transform.LookingAt(nextSparks.GlobalPosition - BulletSpawner.GlobalBasis.Z);
      nextSparks.AmountRatio = GD.Randf();
      nextSparks.Emit();
      _nextParticleIdx = (_nextParticleIdx + 1) % _poolSize;
    }
    else
    {
      _recoilOffset.Position = _recoilOffset.Position.Lerp(to: Vector3.Zero, weight: 20f * (float)delta);
      _recoilOffset.Rotation = _recoilOffset.Rotation.Lerp(to: Vector3.Zero, weight: 20f * (float)delta);
      _delayTimer = Mathf.Max(_delayTimer - (float)delta, 0f);

      _muzzleTimer = Mathf.Max(_muzzleTimer - (float)delta, 0f);
      for (int i = 0; i < _localMuzzleFlashes.Length; i++)
      {
        _localMuzzleFlashes[i].LightEnergy = Mathf.Lerp(0f, GunData.LocalMuzzleFlashEnergy, _muzzleTimer / GunData.MuzzleTime);
        _muzzleFlashSprite.Transparency = Mathf.Lerp(1f, 0f, _muzzleTimer / GunData.MuzzleTime);
      }
      _muzzleFlash.LightEnergy = Mathf.Lerp(0f, _defaultMuzzleEnergy, _muzzleTimer / GunData.MuzzleTime);
    }
  }
}
