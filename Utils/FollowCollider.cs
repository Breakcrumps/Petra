using Godot;

[GlobalClass]
internal sealed partial class FollowCollider : CollisionShape3D
{
  [Export] private Node3D _followedNode = null!;

  public override void _PhysicsProcess(double delta)
    => GlobalPosition = _followedNode.GlobalPosition;
}
