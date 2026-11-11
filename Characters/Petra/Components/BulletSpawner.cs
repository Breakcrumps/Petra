using Godot;
using Petra.Objects.Guns.Bullet;

namespace Petra.Characters.Petra.Components;

[GlobalClass]
internal sealed partial class BulletSpawner : Node3D
{
  [Export] private PackedScene _bulletScene = null!;

  internal int Damage = 100;
  internal float BulletSpeed = 300f;
  internal PhysicsBody3D? Shooter;

  internal void Fire()
  {
    Bullet bullet = _bulletScene.Instantiate<Bullet>();
    GetTree().CurrentScene.AddChild(bullet);
    bullet.GlobalPosition = GlobalPosition;
    bullet.GlobalTransform = bullet.GlobalTransform.LookingAt(GlobalPosition - GlobalBasis.Z);
    bullet.Speed = BulletSpeed;
    bullet.Damage = Damage;

    if (Shooter is null)
      return;

    Vector3 backTarget = GlobalPosition + 1.5f * bullet.GlobalBasis.Z;
    bullet.CheckCollisions(backTarget, GlobalPosition, excludedBody: Shooter, hitBackFaces: true);
  }

  internal void Fire(Vector3 target, int shotsInARow)
  {
    Bullet bullet = _bulletScene.Instantiate<Bullet>();
    GetTree().CurrentScene.AddChild(bullet);
    bullet.GlobalPosition = GlobalPosition;
    bullet.GlobalTransform = bullet.GlobalTransform.LookingAt(target);
    float len = (float)GD.Randfn(0, double.Pi / (24.0 * (shotsInARow + 1)));
    float angle = GD.Randf() * Mathf.Pi;
    bullet.Rotate(Vector3.Right, Mathf.Sin(angle) * len);
    bullet.Rotate(Vector3.Up, Mathf.Cos(angle) * len);
    bullet.Speed = BulletSpeed;
    bullet.Damage = Damage;

    if (Shooter is null)
      return;

    Vector3 backTarget = GlobalPosition + 1.5f * bullet.GlobalBasis.Z;
    bullet.CheckCollisions(backTarget, GlobalPosition, excludedBody: Shooter, hitBackFaces: true);
  }
}
