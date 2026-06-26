using Godot;
using Petra.Characters.Petra;
using Petra.Characters.Petra.Components;

namespace Petra.Guns;

[GlobalClass]
internal sealed partial class GunsWrapper : Node3D
{
  private enum FocusMode { Sight, Target, Squint }
  private FocusMode _focusMode = FocusMode.Target;

  [Export] private SubViewportContainer _mainGunContainer = null!;
  [Export] private PetraCamera _mainCamera = null!;

  [Export] private PackedScene? _gunScene1;
  [Export] private PackedScene? _gunScene2;
  [Export] private PackedScene? _gunScene3;
  [Export] private PackedScene? _gunScene4;
  private readonly GunNode?[] _gunNodes = new GunNode?[4];
  private GunNode? _curGunNode;

  [Export] internal BulletSpawner BulletSpawner = null!;
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

  private PosAndRot _swayOffset = new();
  private PosAndRot _nearWallOffset = new();
  private PosAndRot _bobOffset = new();
  private PosAndRot _recoilOffset = new();
  private PosAndRot _jumpOffset = new();
  private Vector3 _aimOffset;
  private Vector2 _mouseRelative;

  internal bool InAim;
  private bool _inReload;

  public override void _Ready()
  {
    PackedScene?[] gunScenes = [_gunScene1, _gunScene2, _gunScene3, _gunScene4];

    for (int i = 0; i < 4; i++)
    {
      if (gunScenes[i] is not null)
      {
        _gunNodes[i] = gunScenes[i]!.Instantiate<GunNode>();
        _gunNodes[i]!.Chamber();
        _gunNodes[i]!.RefillAmmo();
      }
    }

    for (int i = 0; i < 4; i++)
    {
      if (_gunNodes[i] is not null)
      {
        _curGunNode = _gunNodes[i]!;
        AddChild(_curGunNode);

        BulletSpawner.Damage = _curGunNode.GunData.Damage;
        BulletSpawner.Position = _curGunNode.GunData.BulletSpawnerPos;

        _curGunNode.AnimPlayer.Play("Cock");
        break;
      }
    }

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
  }

  private void CreateLocalMuzzleFlash(int idx, Vector3 offset)
  {
    BulletSpawner.AddChild(_localMuzzleFlashes[idx] = new());
    _localMuzzleFlashes[idx].LightEnergy = 0f;
    _localMuzzleFlashes[idx].Position = offset;
    _localMuzzleFlashes[idx].LightColor = _curGunNode!.GunData.MuzzleFlashColor;
    _localMuzzleFlashes[idx].Layers = 2;
  }

  private void TryLoadData(int idx)
    => TryLoadData(_gunNodes[idx]);

  private void TryLoadData(GunNode? node)
  {
    if (node is null)
      return;

    if (_curGunNode is not null)
      RemoveChild(_curGunNode);

    _curGunNode = node;
    AddChild(_curGunNode);
    BulletSpawner.Position = _curGunNode.GunData.BulletSpawnerPos;
    BulletSpawner.Damage = _curGunNode.GunData.Damage;
    _curGunNode.AnimPlayer.Play("Cock");
  }

  public override void _UnhandledInput(InputEvent @event)
  {
    if (@event is InputEventMouseMotion mouseMotion)
      _mouseRelative = mouseMotion.Relative;
  }

  public override void _PhysicsProcess(double delta)
  {
    if (Input.IsActionJustPressed("SlowDownTime"))
      Engine.TimeScale = Engine.TimeScale == 1f ? .1f : 1f;
    
    if (Input.IsActionJustPressed("Weapon1"))
      TryLoadData(idx: 0);
    else if (Input.IsActionJustPressed("Weapon2"))
      TryLoadData(idx: 1);
    else if (Input.IsActionJustPressed("Weapon3"))
      TryLoadData(idx: 2);
    else if (Input.IsActionJustPressed("Weapon4"))
      TryLoadData(idx: 3);

    if (_curGunNode is null)
      return;
    
    if (_petra.CurrentState == PetraChar.PetraState.Sliding)
    {
      Position = Position.Lerp(to: _curGunNode.GunData.SlidePos, weight: 10f * (float)delta);
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

    HandleReload();

    _mouseRelative = Vector2.Zero;
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
      if (_gunRay.IsColliding() || Input.IsActionPressed("Heap"))
      {
        nextPos = _curGunNode!.GunData.HeapAimPos;
      }
      else
      {
        nextPos = leanDir > 0f ? _curGunNode!.GunData.RightLeanAimPos : _curGunNode!.GunData.LeftLeanAimPos;
        InAim = true;
      }
      float leanAngle = leanDir == 1f ? _curGunNode.GunData.LeanRightAngle : _curGunNode.GunData.LeanLeftAngle;
      nextOrient = Quaternion.FromEuler(new Vector3(0f, 0f, leanAngle));
    }
    else
    {
      if (_gunRay.IsColliding() || Input.IsActionPressed("Heap"))
      {
        nextPos = _curGunNode!.GunData.HeapAimPos;
        nextOrient = _curGunNode.GunData.HeapAimOrient;
      }
      else
      {
        InAim = true;
        nextPos = _curGunNode!.GunData.AimPos;
        nextOrient = _curGunNode.GunData.DefaultOrient;
      }
    }

    nextPos += _bobOffset.Pos + _recoilOffset.Pos + _aimOffset + ((InAim ? .2f : 1f) * _swayOffset.Pos);
    nextOrient *= Quaternion.FromEuler(_recoilOffset.Rot) * Quaternion.FromEuler(_jumpOffset.Rot);
    
    Position = Position.Lerp(to: nextPos, weight: _curGunNode.GunData.AimSpeed * (float)delta);
    Quaternion = Quaternion.Slerp(to: nextOrient, weight: 10f * (float)delta);
  }

  private void HandlePos(double delta)
  {
    if (Input.IsActionPressed("Heap"))
    {
      Vector3 heapPos = Input.IsActionPressed("Down") ? _curGunNode!.GunData.BackRunPos : _curGunNode!.GunData.RunPos;
      Quaternion heapOrient = Input.IsActionPressed("Down") ? _curGunNode.GunData.BackRunOrient : _curGunNode.GunData.RunOrient;
      heapPos += _swayOffset.Pos + _bobOffset.Pos;
      heapOrient *= (
        Quaternion.FromEuler(_bobOffset.Rot)
        * Quaternion.FromEuler(_jumpOffset.Rot)
      );
      Position = Position.Lerp(to: heapPos, weight: _curGunNode.GunData.LeanSpeed * (float)delta);
      Quaternion = Quaternion.Slerp(to: heapOrient, weight: _curGunNode.GunData.LeanSpeed * (float)delta);
      return;
    }
    
    Vector3 nextPos;
    Quaternion nextOrient;
    
    float leanDir = 0f;

    if (Input.IsActionPressed("LeanLeft"))
      leanDir -= 1f;
    if (Input.IsActionPressed("LeanRight"))
      leanDir += 1f;

    Vector3 defaultPos, rightLeanPos, leftLeanPos;

    if (_petra.CurrentState == PetraChar.PetraState.Crouching)
    {
      defaultPos = _curGunNode!.GunData.CrouchPos;
      rightLeanPos = _curGunNode.GunData.CrouchRightLeanPos;
      leftLeanPos = _curGunNode.GunData.CrouchLeftLeanPos;
    }
    else
    {
      defaultPos = _curGunNode!.GunData.DefaultPos;
      rightLeanPos = _curGunNode.GunData.RightLeanPos;
      leftLeanPos = _curGunNode.GunData.LeftLeanPos;
    }

    if (leanDir != 0f)
    {
      nextPos = leanDir > 0f ? rightLeanPos : leftLeanPos;
      float leanAngle = leanDir > 0f ? _curGunNode.GunData.LeanRightAngle : _curGunNode.GunData.LeanLeftAngle;
      nextOrient = Quaternion.FromEuler(new Vector3(0f, 0f, leanAngle));
    }
    else
    {
      if (_petra.CurrentState == PetraChar.PetraState.Running)
      {
        nextPos = Input.IsActionPressed("Down") ? _curGunNode.GunData.BackRunPos : _curGunNode.GunData.RunPos;
        nextOrient = Input.IsActionPressed("Down") ? _curGunNode.GunData.BackRunOrient: _curGunNode.GunData.RunOrient;
      }
      else
      {
        nextPos = defaultPos;
        nextOrient = _curGunNode.GunData.DefaultOrient;
      }
    }

    UpdateNearWallOffsets(delta);

    nextPos += _nearWallOffset.Pos + _swayOffset.Pos + _bobOffset.Pos + _recoilOffset.Pos;
    nextOrient *= (
      Quaternion.FromEuler(_nearWallOffset.Rot)
      * Quaternion.FromEuler(_bobOffset.Rot)
      * Quaternion.FromEuler(_recoilOffset.Rot)
      * Quaternion.FromEuler(_jumpOffset.Rot)
    );

    Position = Position.Lerp(to: nextPos, weight: _curGunNode.GunData.LeanSpeed * (float)delta);
    Quaternion = Quaternion.Slerp(to: nextOrient, weight: _curGunNode.GunData.LeanSpeed * (float)delta);

    _curGunNode.Visible = true;
    _mainGunContainer.Modulate = _mainGunContainer.Modulate.Lerp(to: new Color(1f, 1f, 1f, 1f), weight: 10f * (float)delta);
  }

  private void UpdateNearWallOffsets(double delta)
  {
    Vector3 targetPosOffset = Vector3.Zero, targetOrientation = Vector3.Zero;
    float maxDist = _gunRay.TargetPosition.Length(); 

    if (_gunRay.IsColliding())
    {
      float collisionDist = _gunRay.GetCollisionPoint().DistanceTo(_gunRay.GlobalPosition);
      float proximity = Mathf.Clamp(1.0f - (collisionDist / maxDist), 0f, 1f);
      
      targetPosOffset = proximity * (_curGunNode!.GunData.NearWallPos - _curGunNode.GunData.DefaultPos);
      targetOrientation = proximity * _curGunNode.GunData.NearWallRot;
    }

    _nearWallOffset.Pos = _nearWallOffset.Pos.Lerp(to: targetPosOffset, weight: _curGunNode!.GunData.PullBackSpeed * (float)delta);
    _nearWallOffset.Rot = _nearWallOffset.Rot.Lerp(to: targetOrientation, weight: _curGunNode.GunData.PullBackSpeed * (float)delta);
  }

  private void UpdateSwayOffsets(double delta)
  {
    float targetSwayX = -_mouseRelative.X * _curGunNode!.GunData.SwayAmount;
    float targetSwayY = _mouseRelative.Y * _curGunNode.GunData.SwayAmount;

    targetSwayX = Mathf.Clamp(targetSwayX, -_curGunNode.GunData.SwayThreshold, _curGunNode.GunData.SwayThreshold);
    targetSwayY = Mathf.Clamp(targetSwayY, -_curGunNode.GunData.SwayThreshold, _curGunNode.GunData.SwayThreshold);

    Vector3 targetSway = new(targetSwayX, targetSwayY, 0);

    _swayOffset.Pos = _swayOffset.Pos.Lerp(targetSway, _curGunNode.GunData.SwayLerpSpeed * (float)delta);
  }

  private void UpdateBobOffsets(double delta)
  {
    if (_petra.TimeMoving != 0f)
    {
      float bobCoef = Input.IsActionPressed("Aim") ? .3f : 1f;
      _bobOffset.Pos.Y = bobCoef * -_curGunNode!.GunData.BobAmp * Mathf.Abs(Mathf.Sin(_camera.BobFreq * _petra.TimeMoving));
      _bobOffset.Pos.X = bobCoef * -_curGunNode.GunData.LeftRightAmp * Mathf.Abs(Mathf.PosMod(_petra.TimeMoving - Mathf.Pi / _camera.BobFreq,  2f * Mathf.Pi / _camera.BobFreq) - Mathf.Pi / _camera.BobFreq);
      _bobOffset.Rot.X = bobCoef * _curGunNode.GunData.BobRotAmp * Mathf.Abs(Mathf.Sin(_camera.BobFreq * (_petra.TimeMoving + .01f * _camera.BobFreq)));
    }
    else
    {
      _bobOffset.Pos = _bobOffset.Pos.Lerp(to: Vector3.Zero, weight: _curGunNode!.GunData.ReturnToPosSpeed * (float)delta);
      _bobOffset.Rot = _bobOffset.Rot.Lerp(to: Vector3.Zero, weight: _curGunNode.GunData.ReturnToPosSpeed * (float)delta);
    }
  }

  private void UpdateJumpOffsets(double delta)
  {
    if (_petra.Velocity.Y != 0f)
    {
      float targetRotOffset = -Mathf.Clamp(_petra.Velocity.Y, -10f, 10f) * Mathf.Pi / 200f;
      _jumpOffset.Rot = _jumpOffset.Rot.Lerp(
        to: _jumpOffset.Rot with { X = targetRotOffset },
        weight: 50f * (float)delta
      );
    }
    else
    {
      _jumpOffset.Rot = _jumpOffset.Rot.Lerp(to: Vector3.Zero, weight: 10f * (float)delta);
    }
  }

  private void HandleFire(double delta)
  {
    if (Input.IsActionPressed("Heap") && !Input.IsActionPressed("Aim") || _curGunNode!.CartridgesChambered == 0)
    {
      HandleRecoil(delta);
      return;
    }
    
    if (
      Input.IsActionJustPressed("Fire")
      && (Input.IsActionPressed("Aim")
        || !_gunRay.IsColliding()
        && _petra.CurrentState != PetraChar.PetraState.Running
      )
      && _delayTimer == 0f
    )
      Fire();
    else
      HandleRecoil(delta);
  }

  private void Fire()
  {
    BulletSpawner.Fire();

    if (_curGunNode!.CartridgesInMag == 0)
    {
      _curGunNode.AnimPlayer.Play("ShootLast");
      _curGunNode.CartridgesChambered = 0;
    }
    else
    {
      _curGunNode.AnimPlayer.Play("Shoot");
      _curGunNode.CartridgesInMag--;
    }

    _recoilOffset.Pos = Input.IsActionPressed("Aim") ? _curGunNode.GunData!.AimRecoilOffsetTarget.Pos : _curGunNode.GunData!.RecoilOffsetTarget.Pos;
    _recoilOffset.Rot = Input.IsActionPressed("Aim") ? _curGunNode.GunData.AimRecoilOffsetTarget.Rot : _curGunNode.GunData.RecoilOffsetTarget.Rot;
    _recoilOffset.Pos.X = Input.IsActionPressed("Aim") ? (GD.Randf() - .5f) / 12f : (GD.Randf() - .5f) / 4f;
    _recoilOffset.Rot.Y = Input.IsActionPressed("Aim") ? (GD.Randf() - .5f) / 12f : (GD.Randf() - .5f) / 4f;

    if (_petra.CurrentState == PetraChar.PetraState.Crouching)
    {
      _recoilOffset.Pos.Y *= .5f;
      _recoilOffset.Rot.X *= .5f;
    }

    _delayTimer = _curGunNode.GunData.DelayTime;
    _muzzleTimer = _curGunNode.GunData.MuzzleTime;
    _muzzleFlash.LightEnergy = _defaultMuzzleEnergy;

    for (int i = 0; i < _localMuzzleFlashes.Length; i++)
      _localMuzzleFlashes[i].LightEnergy = _curGunNode.GunData.LocalMuzzleFlashEnergy;

    _muzzleFlashSprite.Visible = true;

    GpuParticles3D nextSmoke = _smokePool[_nextParticleIdx];
    nextSmoke.GlobalPosition = BulletSpawner.GlobalPosition;
    nextSmoke.Transform = nextSmoke.Transform.LookingAt(nextSmoke.GlobalPosition - BulletSpawner.GlobalBasis.Z);
    nextSmoke.Restart();
    nextSmoke.AmountRatio = GD.Randf();
    nextSmoke.Emitting = true;

    DelayedParticles nextSparks = _sparksPool[_nextParticleIdx];
    nextSparks.GlobalPosition = BulletSpawner.GlobalPosition;
    nextSparks.Transform = nextSparks.Transform.LookingAt(nextSparks.GlobalPosition - BulletSpawner.GlobalBasis.Z);
    nextSparks.AmountRatio = GD.Randf();
    nextSparks.Emit();
    _nextParticleIdx = (_nextParticleIdx + 1) % _poolSize;
  }

  private void HandleRecoil(double delta)
  {
    _recoilOffset.Pos = _recoilOffset.Pos.Lerp(to: Vector3.Zero, weight: 20f * (float)delta);
    _recoilOffset.Rot = _recoilOffset.Rot.Lerp(to: Vector3.Zero, weight: 20f * (float)delta);
    _delayTimer = Mathf.Max(_delayTimer - (float)delta, 0f);

    _muzzleTimer = Mathf.Max(_muzzleTimer - (float)delta, 0f);
    for (int i = 0; i < _localMuzzleFlashes.Length; i++)
    {
      _localMuzzleFlashes[i].LightEnergy = Mathf.Lerp(0f, _curGunNode!.GunData.LocalMuzzleFlashEnergy, _muzzleTimer / _curGunNode.GunData.MuzzleTime);
      _muzzleFlashSprite.Transparency = Mathf.Lerp(1f, 0f, _muzzleTimer / _curGunNode.GunData.MuzzleTime);
    }
    _muzzleFlash.LightEnergy = Mathf.Lerp(0f, _defaultMuzzleEnergy, _muzzleTimer / _curGunNode!.GunData.MuzzleTime);
  }

  private void HandleReload()
  {
    if (!Input.IsActionJustPressed("Reload"))
      return;

    _inReload = true;

    if (_curGunNode!.CartridgesChambered == 0)
      _curGunNode.AnimPlayer.Play("ReloadLast");
    else
      _curGunNode.AnimPlayer.Play("Reload");
  }
}
