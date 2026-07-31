using Godot;

namespace Petra.Characters.Enemy;

internal sealed partial class EnemyAnimTree : AnimationTree
{
  [Export] private MovementController _movementController = null!;
  
  private AnimationNodeStateMachinePlayback _animStateMachine = null!;

  public override void _Ready()
    => _animStateMachine = (AnimationNodeStateMachinePlayback)(GodotObject)Get("parameters/playback");
  
  public override void _PhysicsProcess(double delta)
  {
    string nextAnimNodeName = NextAnimNodeName();

    if (nextAnimNodeName != _animStateMachine.GetCurrentNode())
      _animStateMachine.Travel(nextAnimNodeName);
  }

  private string NextAnimNodeName()
  {
    if (_movementController.GroundMoveDirLocal == Vector2.Zero)
      return "Idle";
    
    if (_movementController.SpeedMode == MovementController.SpeedType.Run)
      return "Run";

    Set("parameters/Walk/blend_position", _movementController.GroundMoveDirLocal);
    return "Walk";
  }
}
