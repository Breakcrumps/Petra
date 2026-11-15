using Godot;
using Petra.Characters;
using Petra.Types;
using Petra.Utils.BulletReactors;

internal sealed partial class MeleeArea : Area3D
{
  [Export] private int _meleeDamage = 70;

  private CollisionShape3D _collisionShape = null!;
  
  public override void _Ready()
  {
    _collisionShape = GetChild<CollisionShape3D>(0);

    BodyEntered += node =>
    {
      if (node is IDamageable damageable)
        damageable.TakeDamage(new Attack(_meleeDamage));
      if (node is IBreakable breakable)
        _ = breakable.GetBreaker().Break(_collisionShape.GlobalPosition, -_collisionShape.GlobalBasis.Z);
    };
  }
}
