using Godot;

internal sealed partial class BulletSpawner : Node3D
{
  internal PetraCamera Camera = null!;
  [Export] private float _bulletSpeed = 100f;
  [Export] private PackedScene _bulletScene = null!;
  [Export] private Gun _gun = null!;

  public override void _PhysicsProcess(double delta)
  {
    if (!Input.IsActionJustPressed("Fire"))
      return;

    Bullet bullet = _bulletScene.Instantiate<Bullet>();
    bullet.Direction = -Camera.Basis.Z;
    bullet.Speed = _bulletSpeed;
    bullet.Damage = _gun.Damage;
    GetTree().CurrentScene.AddChild(bullet);
    bullet.GlobalPosition = GlobalPosition + Camera.GlobalPosition;
  }
}
