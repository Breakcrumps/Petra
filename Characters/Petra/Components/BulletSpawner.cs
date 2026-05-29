using System;
using Godot;
using Petra.Resources.Objects.Guns;

namespace Petra.Characters.Petra.Components;

[GlobalClass]
internal sealed partial class BulletSpawner : Node3D
{
  [Export] private PetraChar _petra = null!;
  [Export] private float _bulletSpeed = 800f;
  [Export] private PackedScene _bulletScene = null!;
  [Export] private Gun _gun = null!;

  internal void Fire()
  {
    Bullet bullet = _bulletScene.Instantiate<Bullet>();
    GetTree().CurrentScene.AddChild(bullet);
    bullet.GlobalPosition = GlobalPosition;
    bullet.GlobalTransform = bullet.GlobalTransform.LookingAt(GlobalPosition - _gun.GlobalBasis.Z);
    bullet.Speed = _bulletSpeed;
    bullet.Damage = _gun.GunData.Damage;
    bullet.Petra = _petra;

    Vector3 backTarget = GlobalPosition + 2f * bullet.GlobalBasis.Z;
    bullet.CheckCollisions(backTarget, GlobalPosition, excludePetra: true, hitBackFaces: true);
  }
}
