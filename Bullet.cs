using Godot;

internal sealed partial class Bullet : Area3D
{
  [Export] private float _secsToLive = 3f;
  private float _secsLived;

  internal int Damage { private get; set; }

  internal float Speed { private get; set; }
  internal Vector3 Direction { private get; set; }

  public override void _Ready()
  {
    BodyEntered += TryDamageBody;
    GlobalTransform = GlobalTransform.LookingAt(GlobalPosition + Direction);
  }

  private void TryDamageBody(Node3D body)
  {
    if (body is IDamageable damageable)
      damageable.TakeDamage(new Attack { Damage = Damage });

    QueueFree();
  }

  public override void _PhysicsProcess(double delta)
  {
    GlobalPosition += Direction * Speed * (float)delta;
    
    _secsLived += (float)delta;

    if (_secsLived >= _secsToLive)
      QueueFree();
  }
}
