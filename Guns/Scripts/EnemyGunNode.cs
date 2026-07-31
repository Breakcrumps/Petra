using Godot;
using Petra.Characters.Petra.Components;
using Petra.Objects.Guns.Scripts;

namespace Petra.Guns.Scripts;

internal sealed partial class EnemyGunNode : GunNode
{
  [Export] private CharacterBody3D _shooter = null!;
  [Export] private BulletSpawner _bulletSpawner = null!;

  public override void _Ready()
  {
    _bulletSpawner.BulletSpeed = GunData.BulletSpeed;
    _bulletSpawner.Shooter = _shooter;
  }

  internal void Fire()
    => _bulletSpawner.Fire();
}
