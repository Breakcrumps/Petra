using Godot;

namespace Petra.Characters.Enemy;

internal sealed partial class EnemyAnimTree : AnimationTree
{
  [Export] private MovementController _movementController = null!;
  
  public override void _PhysicsProcess(double delta)
  {
    bool moving = _movementController.GroundMoveDirLocal != Vector2.Zero;
    Set("parameters/conditions/idle", !moving);
    Set("parameters/conditions/walk", moving);
    Set("parameters/Walk/blend_position", _movementController.GroundMoveDirLocal);
  }
}
