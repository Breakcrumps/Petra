using System;
using Godot;
using Petra.Characters.Petra;
using Petra.Characters.Petra.Components;

namespace Petra.Guns;

[GlobalClass]
internal sealed partial class Gun : Node3D
{
  [Export] private GunData? _gunData1;
  [Export] private GunData? _gunData2;
  [Export] private GunData? _gunData3;
  [Export] private GunData? _gunData4;
  private GunData?[] _gunDatas = new GunData?[4];
  internal GunData? CurrentGunData;
  private Node3D?[] _gunNodes = new Node3D?[4];
  private Node3D? _currentGunNode;
  private AnimationPlayer? _currentGunAnimPlayer;

  private int _curCartridges;

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

  [Obsolete]
  private MeshInstance3D _meshNode = null!;

  private PosAndRot _swayOffset = new();
  private PosAndRot _nearWallOffset = new();
  private PosAndRot _bobOffset = new();
  private PosAndRot _recoilOffset = new();
  private PosAndRot _jumpOffset = new();
  private Vector2 _mouseRelative;

  internal bool InAim;
  private bool _inReload;

  public override void _Ready()
  {
    _gunDatas = [_gunData1, _gunData2, _gunData3, _gunData4];
    
    if (_gunData1 is not null)
      _gunNodes[0] = _gunData1.GunScene.Instantiate<Node3D>();
    if (_gunData2 is not null)
      _gunNodes[1] = _gunData2.GunScene.Instantiate<Node3D>();
    if (_gunData3 is not null)
      _gunNodes[2] = _gunData3.GunScene.Instantiate<Node3D>();
    if (_gunData4 is not null)
      _gunNodes[3] = _gunData4.GunScene.Instantiate<Node3D>();

    for (int i = 0; i < 4; i++)
    {
      if (_gunDatas[i] is not null)
      {
        CurrentGunData = _gunDatas[i];
        _currentGunNode = _gunNodes[i];
        AddChild(_currentGunNode);
        _currentGunAnimPlayer = _currentGunNode!.GetNode<AnimationPlayer>("AnimationPlayer");
        _currentGunAnimPlayer.Play("Cock");
        _curCartridges = CurrentGunData!.MaxCartridges;
        break;
      }
    }
    
    if (CurrentGunData is not null)
      BulletSpawner.Position = CurrentGunData.BulletSpawnerPos;

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
    _localMuzzleFlashes[idx].LightColor = CurrentGunData!.MuzzleFlashColor;
    _localMuzzleFlashes[idx].Layers = 2;
  }

  private void TryLoadData(int idx)
    => TryLoadData(_gunDatas[idx], _gunNodes[idx]);

  private void TryLoadData(GunData? data, Node3D? node)
  {
    if (node is null || data is null)
      return;
    
    CurrentGunData = data;
    BulletSpawner.Position = CurrentGunData.BulletSpawnerPos;

    if (_currentGunNode is not null)
      RemoveChild(_currentGunNode);
    _currentGunNode = node;
    _currentGunAnimPlayer = _currentGunNode.GetNode<AnimationPlayer>("AnimationPlayer");
    AddChild(_currentGunNode);
    _currentGunAnimPlayer.Play("Cock");
    _curCartridges = CurrentGunData.MaxCartridges;
  }

  public override void _UnhandledInput(InputEvent @event)
  {
    if (@event is InputEventMouseMotion mouseMotion)
      _mouseRelative = mouseMotion.Relative;
  }

  public override void _PhysicsProcess(double delta)
  {
    if (Input.IsActionJustPressed("Weapon1"))
      TryLoadData(idx: 0);
    else if (Input.IsActionJustPressed("Weapon2"))
      TryLoadData(idx: 1);
    else if (Input.IsActionJustPressed("Weapon3"))
      TryLoadData(idx: 2);
    else if (Input.IsActionJustPressed("Weapon4"))
      TryLoadData(idx: 3);

    if (CurrentGunData is null)
      return;
    
    if (_petra.CurrentState == PetraChar.PetraState.Sliding)
    {
      Position = Position.Lerp(to: CurrentGunData.SlidePos, weight: 10f * (float)delta);
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
        nextPos = CurrentGunData!.HeapAimPos;
      }
      else
      {
        nextPos = leanDir > 0f ? CurrentGunData!.RightLeanAimPos : CurrentGunData!.LeftLeanAimPos;
        InAim = true;
      }
      float leanAngle = leanDir == 1f ? CurrentGunData.LeanRightAngle : CurrentGunData.LeanLeftAngle;
      nextOrient = Quaternion.FromEuler(new Vector3(0f, 0f, leanAngle));
    }
    else
    {
      if (_gunRay.IsColliding() || Input.IsActionPressed("Heap"))
      {
        nextPos = CurrentGunData!.HeapAimPos;
        nextOrient = CurrentGunData.HeapAimOrient;
      }
      else
      {
        InAim = true;
        nextPos = CurrentGunData!.AimPos;
        nextOrient = CurrentGunData.DefaultOrient;
      }
    }

    nextPos += _bobOffset.Position + _swayOffset.Position + _recoilOffset.Position;
    nextOrient *= Quaternion.FromEuler(_recoilOffset.Rotation) * Quaternion.FromEuler(_jumpOffset.Rotation);
    
    Position = Position.Lerp(to: nextPos, weight: CurrentGunData.AimSpeed * (float)delta);
    Quaternion = Quaternion.Slerp(to: nextOrient, weight: 10f * (float)delta);
  }

  private void HandlePos(double delta)
  {
    if (Input.IsActionPressed("Heap"))
    {
      Vector3 heapPos = Input.IsActionPressed("Down") ? CurrentGunData!.BackRunPos : CurrentGunData!.RunPos;
      Quaternion heapOrient = Input.IsActionPressed("Down") ? CurrentGunData.BackRunOrient : CurrentGunData.RunOrient;
      heapPos += _swayOffset.Position + _bobOffset.Position;
      heapOrient *= (
        Quaternion.FromEuler(_bobOffset.Rotation)
        * Quaternion.FromEuler(_jumpOffset.Rotation)
      );
      Position = Position.Lerp(to: heapPos, weight: CurrentGunData.LeanSpeed * (float)delta);
      Quaternion = Quaternion.Slerp(to: heapOrient, weight: CurrentGunData.LeanSpeed * (float)delta);
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
      defaultPos = CurrentGunData!.CrouchPos;
      rightLeanPos = CurrentGunData.CrouchRightLeanPos;
      leftLeanPos = CurrentGunData.CrouchLeftLeanPos;
    }
    else
    {
      defaultPos = CurrentGunData!.DefaultPos;
      rightLeanPos = CurrentGunData.RightLeanPos;
      leftLeanPos = CurrentGunData.LeftLeanPos;
    }

    if (leanDir != 0f)
    {
      nextPos = leanDir > 0f ? rightLeanPos : leftLeanPos;
      float leanAngle = leanDir > 0f ? CurrentGunData.LeanRightAngle : CurrentGunData.LeanLeftAngle;
      nextOrient = Quaternion.FromEuler(new Vector3(0f, 0f, leanAngle));
    }
    else
    {
      if (_petra.CurrentState == PetraChar.PetraState.Running)
      {
        nextPos = Input.IsActionPressed("Down") ? CurrentGunData.BackRunPos : CurrentGunData.RunPos;
        nextOrient = Input.IsActionPressed("Down") ? CurrentGunData.BackRunOrient: CurrentGunData.RunOrient;
      }
      else
      {
        nextPos = defaultPos;
        nextOrient = CurrentGunData.DefaultOrient;
      }
    }

    UpdateNearWallOffsets(delta);

    nextPos += _nearWallOffset.Position + _swayOffset.Position + _bobOffset.Position + _recoilOffset.Position;
    nextOrient *= (
      Quaternion.FromEuler(_nearWallOffset.Rotation)
      * Quaternion.FromEuler(_bobOffset.Rotation)
      * Quaternion.FromEuler(_recoilOffset.Rotation)
      * Quaternion.FromEuler(_jumpOffset.Rotation)
    );

    Position = Position.Lerp(to: nextPos, weight: CurrentGunData.LeanSpeed * (float)delta);
    Quaternion = Quaternion.Slerp(to: nextOrient, weight: CurrentGunData.LeanSpeed * (float)delta);
  }

  private void UpdateNearWallOffsets(double delta)
  {
    Vector3 targetPosOffset = Vector3.Zero, targetOrientation = Vector3.Zero;
    float maxDist = _gunRay.TargetPosition.Length(); 

    if (_gunRay.IsColliding())
    {
      float collisionDist = _gunRay.GetCollisionPoint().DistanceTo(_gunRay.GlobalPosition);
      float proximity = Mathf.Clamp(1.0f - (collisionDist / maxDist), 0f, 1f);
      
      targetPosOffset = proximity * (CurrentGunData!.NearWallPos - CurrentGunData.DefaultPos);
      targetOrientation = proximity * CurrentGunData.NearWallRot;
    }

    _nearWallOffset.Position = _nearWallOffset.Position.Lerp(to: targetPosOffset, weight: CurrentGunData!.PullBackSpeed * (float)delta);
    _nearWallOffset.Rotation = _nearWallOffset.Rotation.Lerp(to: targetOrientation, weight: CurrentGunData.PullBackSpeed * (float)delta);
  }

  private void UpdateSwayOffsets(double delta)
  {
    if (_mouseRelative.LengthSquared() < .5f)
      _mouseRelative = Vector2.Zero;
    else if ((_mouseRelative.X > 0 && _swayOffset.Position.X > 0) || (_mouseRelative.X < 0 && _swayOffset.Position.X < 0))
      _mouseRelative.X = 0;
    
    float targetSwayX = -_mouseRelative.X * CurrentGunData!.SwayAmount;
    float targetSwayY = _mouseRelative.Y * CurrentGunData.SwayAmount;

    targetSwayX = Mathf.Clamp(targetSwayX, -CurrentGunData.SwayThreshold, CurrentGunData.SwayThreshold);
    targetSwayY = Mathf.Clamp(targetSwayY, -CurrentGunData.SwayThreshold, CurrentGunData.SwayThreshold);

    Vector3 targetSway = new(targetSwayX, targetSwayY, 0);

    _swayOffset.Position = _swayOffset.Position.Lerp(targetSway, CurrentGunData.SwayLerpSpeed * (float)delta);

    _mouseRelative *= .2f;
  }

  private void UpdateBobOffsets(double delta)
  {
    if (_petra.TimeMoving != 0f)
    {
      float bobCoef = Input.IsActionPressed("Aim") ? .3f : 1f;
      _bobOffset.Position.Y = bobCoef * -CurrentGunData!.BobAmp * Mathf.Abs(Mathf.Sin(_camera.BobFreq * _petra.TimeMoving));
      _bobOffset.Position.X = bobCoef * -CurrentGunData.LeftRightAmp * Mathf.Abs(Mathf.PosMod(_petra.TimeMoving - Mathf.Pi / _camera.BobFreq,  2f * Mathf.Pi / _camera.BobFreq) - Mathf.Pi / _camera.BobFreq);
      _bobOffset.Rotation.X = bobCoef * CurrentGunData.BobRotAmp * Mathf.Abs(Mathf.Sin(_camera.BobFreq * (_petra.TimeMoving + .01f * _camera.BobFreq)));
    }
    else
    {
      _bobOffset.Position = _bobOffset.Position.Lerp(to: Vector3.Zero, weight: CurrentGunData!.ReturnToPosSpeed * (float)delta);
      _bobOffset.Rotation = _bobOffset.Rotation.Lerp(to: Vector3.Zero, weight: CurrentGunData.ReturnToPosSpeed * (float)delta);
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
    if (Input.IsActionPressed("Heap") && !Input.IsActionPressed("Aim") || _curCartridges == 0)
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
    if (--_curCartridges != 0)
      _currentGunAnimPlayer!.Play("Shoot");
    else
      _currentGunAnimPlayer!.Play("ShootLast");
    _recoilOffset.Position = Input.IsActionPressed("Aim") ? CurrentGunData!.AimRecoilOffsetTarget.Position : CurrentGunData!.RecoilOffsetTarget.Position;
    _recoilOffset.Rotation = Input.IsActionPressed("Aim") ? CurrentGunData.AimRecoilOffsetTarget.Rotation : CurrentGunData.RecoilOffsetTarget.Rotation;
    _recoilOffset.Position.X = Input.IsActionPressed("Aim") ? (GD.Randf() - .5f) / 12f : (GD.Randf() - .5f) / 4f;
    _recoilOffset.Rotation.Y = Input.IsActionPressed("Aim") ? (GD.Randf() - .5f) / 12f : (GD.Randf() - .5f) / 4f;
    if (_petra.CurrentState == PetraChar.PetraState.Crouching)
    {
      _recoilOffset.Position.Y *= .5f;
      _recoilOffset.Rotation.X *= .5f;
    }
    _delayTimer = CurrentGunData.DelayTime;
    _muzzleTimer = CurrentGunData.MuzzleTime;
    _muzzleFlash.LightEnergy = _defaultMuzzleEnergy;
    for (int i = 0; i < _localMuzzleFlashes.Length; i++)
      _localMuzzleFlashes[i].LightEnergy = CurrentGunData.LocalMuzzleFlashEnergy;
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
    _recoilOffset.Position = _recoilOffset.Position.Lerp(to: Vector3.Zero, weight: 20f * (float)delta);
    _recoilOffset.Rotation = _recoilOffset.Rotation.Lerp(to: Vector3.Zero, weight: 20f * (float)delta);
    _delayTimer = Mathf.Max(_delayTimer - (float)delta, 0f);

    _muzzleTimer = Mathf.Max(_muzzleTimer - (float)delta, 0f);
    for (int i = 0; i < _localMuzzleFlashes.Length; i++)
    {
      _localMuzzleFlashes[i].LightEnergy = Mathf.Lerp(0f, CurrentGunData!.LocalMuzzleFlashEnergy, _muzzleTimer / CurrentGunData.MuzzleTime);
      _muzzleFlashSprite.Transparency = Mathf.Lerp(1f, 0f, _muzzleTimer / CurrentGunData.MuzzleTime);
    }
    _muzzleFlash.LightEnergy = Mathf.Lerp(0f, _defaultMuzzleEnergy, _muzzleTimer / CurrentGunData!.MuzzleTime);
  }

  private void HandleReload()
  {
    if (!Input.IsActionJustPressed("Reload"))
      return;

    _inReload = true;

    if (_curCartridges == 0)
      _currentGunAnimPlayer!.Play("ReloadLast");
    else
      _currentGunAnimPlayer!.Play("Reload");

    _curCartridges = CurrentGunData!.MaxCartridges;
  }
}
