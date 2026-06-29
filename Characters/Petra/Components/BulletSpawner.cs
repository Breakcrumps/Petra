using Godot;
using Petra.Objects.Guns.Bullet;

namespace Petra.Characters.Petra.Components;

[GlobalClass]
internal sealed partial class BulletSpawner : Node3D
{
  [Export] private PhysicsBody3D? _shooter;
  [Export] private float _bulletSpeed = 300f;
  [Export] private PackedScene _bulletScene = null!;

  internal int Damage = 100;

  internal void Fire()
  {
    Bullet bullet = _bulletScene.Instantiate<Bullet>();
    GetTree().CurrentScene.AddChild(bullet);
    bullet.GlobalPosition = GlobalPosition;
    bullet.GlobalTransform = bullet.GlobalTransform.LookingAt(GlobalPosition - GlobalBasis.Z);
    bullet.Speed = _bulletSpeed;
    bullet.Damage = Damage;

    if (_shooter is null)
      return;

    Vector3 backTarget = GlobalPosition + 1.5f * bullet.GlobalBasis.Z;
    bullet.CheckCollisions(backTarget, GlobalPosition, excludedBody: _shooter, hitBackFaces: true);
  }
}
