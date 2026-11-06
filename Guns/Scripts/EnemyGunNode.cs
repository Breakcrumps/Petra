using Godot;
using Petra.Characters.Petra.Components;
using Petra.Objects.Guns.Scripts;
using Petra.Static;

namespace Petra.Guns.Scripts;

[GlobalClass]
internal sealed partial class EnemyGunNode : GunNode
{
  [Export] private BulletSpawner _bulletSpawner = null!;
  [Export] private Marker3D _handMarker = null!;
  [Export] private Marker3D _defaultMarker = null!;
  [Export] private Marker3D _holdPos = null!;
  [Export] private PackedScene _smokeScene = null!;
  [Export] private PackedScene _sparksScene = null!;
  [Export] private Texture2D _muzzleFlashImage = null!;
  [Export] private OmniLight3D _muzzleFlash = null!;

  private GpuParticles3D _smoke = null!;
  private DelayedParticles _sparks = null!;
  private Sprite3D _muzzleFlashSprite = null!;

  private float _flashTimer;

  internal bool InHand;

  public override void _Ready()
  {
    _bulletSpawner.BulletSpeed = GunData.BulletSpeed;
    _smoke = _smokeScene.Instantiate<GpuParticles3D>();
    AddChild(_smoke);
    _sparks = _sparksScene.Instantiate<DelayedParticles>();
    AddChild(_sparks);
    _muzzleFlashSprite = new()
    {
      Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
      Transparency = 1f,
      Layers = 1,
      Texture = _muzzleFlashImage,
      Scale = new Vector3(.25f, .25f, .25f)
    };
    _bulletSpawner.AddChild(_muzzleFlashSprite);
    _muzzleFlash.LightColor = GunData.MuzzleFlashColor;
    _muzzleFlash.LightEnergy = 0f;
    CartridgesChambered = GunData.CartridgesChambered;
    CartridgesInMag = GunData.CartridgesInMag;
  }

  internal void Fire()
  {
    _bulletSpawner.Fire(GlobalInstances.Petra.HeadPoint.GlobalPosition);
    _smoke.GlobalPosition = _bulletSpawner.GlobalPosition;
    _smoke.Transform = _smoke.Transform.LookingAt(_smoke.GlobalPosition - _bulletSpawner.GlobalBasis.Z);
    _smoke.Restart();
    _smoke.AmountRatio = GD.Randf();
    _smoke.Emitting = true;
    _smoke.GlobalPosition = _bulletSpawner.GlobalPosition;
    _sparks.Transform = _sparks.Transform.LookingAt(_sparks.GlobalPosition - _bulletSpawner.GlobalBasis.Z);
    _sparks.Restart();
    _sparks.AmountRatio = GD.Randf();
    _sparks.Emitting = true;
    _muzzleFlashSprite.Transparency = .5f;
    _muzzleFlash.LightEnergy = 5f;
    _flashTimer = GunData.MuzzleTime;
    if (CartridgesInMag > 0)
    {
      CartridgesInMag--;
      AnimPlayer.Play("Shoot");
    }
    else
    {
      CartridgesChambered = 0;
      AnimPlayer.Play("ShootLast");
    }
  }

  public override void _PhysicsProcess(double delta)
  {
    if (InHand)
    {
      GlobalRotation = _handMarker.GlobalRotation;
      GlobalPosition = _handMarker.GlobalPosition - (GlobalBasis * _holdPos.Position);
    }
    else
    {
      GlobalPosition = _defaultMarker.GlobalPosition;
      GlobalRotation = _defaultMarker.GlobalRotation;
    }

    _flashTimer = Mathf.Max(_flashTimer - (float)delta, 0f);

    if (_flashTimer == 0f)
    {
      _muzzleFlashSprite.Transparency = 1f;
      _muzzleFlash.LightEnergy = 0f;
    }
  }
}
