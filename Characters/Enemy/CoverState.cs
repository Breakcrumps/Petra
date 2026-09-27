using Godot;
using Petra.Static;
using Petra.Utils.Cover;

namespace Petra.Characters.Enemy;

[GlobalClass]
internal sealed partial class CoverState : State
{
  [Export] private MovementController _movementController = null!;
  [Export] private ShootController _shootController = null!;
  [Export] private NavigationAgent3D _navAgent = null!;

  [ExportGroup("TransitableStates")]
  [Export] private WalkToPatrolState _walkToPatrolState = null!;

  private StateMachine _stateMachine = null!;
  private EnemyChar _parentChar = null!;

  public override void _Ready()
  {
    _stateMachine = GetParent<StateMachine>();
    _parentChar = _stateMachine.GetParent<EnemyChar>();
  }
  
  internal override void Enter()
  {
    if (GlobalInstances.CoverManager.GetBestCover(_parentChar, GlobalInstances.Petra) is null)
    {
      _stateMachine.Transition(_walkToPatrolState);
      return;
    }
    
    _movementController.ControlMode = MovementController.ControlType.PositionDirection;
    _movementController.SpeedMode = MovementController.SpeedType.Run;
  }

  internal override void Exit()
    => _shootController.Deactivate();

  internal override void PhysicsProcess(double delta)
  {
    CoverMarker? bestCover = GlobalInstances.CoverManager.GetBestCover(_parentChar, GlobalInstances.Petra);

    if (bestCover is null)
    {
      _stateMachine.Transition(_walkToPatrolState);
      return;
    }

    _navAgent.TargetPosition = bestCover.GlobalPosition;

    if (!_navAgent.IsTargetReachable())
      return;

    Vector3 difVector = _navAgent.GetNextPathPosition() with { Y = 0f } - _parentChar.GlobalPosition;

    if (difVector.LengthSquared() < .01f)
    {
      _movementController.Direction = Vector3.Zero;
      _parentChar.Quaternion =_parentChar.Quaternion.Slerp(
        Basis.LookingAt(bestCover.GlobalBasis.Z).GetRotationQuaternion(),
        10f * (float)delta
      );
      bestCover.Occupant = _parentChar;
      _shootController.Activate();
      return;
    }

    _movementController.Direction = difVector.Normalized();
    _parentChar.Quaternion = _parentChar.Quaternion.Slerp(
      Basis.LookingAt((GlobalInstances.Petra.GlobalPosition - _parentChar.GlobalPosition) with { Y = 0f }).GetRotationQuaternion(),
      10f * (float)delta
    );
    _shootController.Deactivate();
  }
}
