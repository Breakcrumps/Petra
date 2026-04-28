using Godot;
using Petra.Resources.Objects.Guns;

namespace Petra.Characters.Petra.Components;

[GlobalClass]
internal sealed partial class BulletSpawner : Node3D
{
  internal PetraCamera Camera = null!;
  [Export] private float _bulletSpeed = 800f;
  [Export] private PackedScene _bulletScene = null!;
  [Export] private Gun _gun = null!;

  internal void Fire()
  {
    Bullet bullet = _bulletScene.Instantiate<Bullet>();
    GetTree().CurrentScene.AddChild(bullet);
    bullet.GlobalPosition = GlobalPosition + Camera.GlobalPosition;
    bullet.GlobalTransform = bullet.GlobalTransform.LookingAt(bullet.GlobalPosition - _gun.GlobalBasis.Z);
    bullet.Speed = _bulletSpeed;
    bullet.Damage = _gun.GunData.Damage;
  }
}
