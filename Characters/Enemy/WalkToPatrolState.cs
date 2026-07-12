using Godot;

namespace Petra.Characters.Enemy;

[GlobalClass]
internal sealed partial class WalkToPatrolState : State
{
  [Export] private MovementController _movementController = null!;
  [Export] private NavigationAgent3D _navAgent = null!;
  
  private EnemyChar _parentChar = null!;
  private PathFollow3D? _pathFollow;
  private StateMachine _stateMachine = null!;

  [ExportGroup("TransitableStates")]
  [Export] private CoverState _coverState = null!;
  [Export] private PatrolState _patrolState = null!;
  
  public override void _Ready()
  {
    _stateMachine = GetParent<StateMachine>();
    _parentChar = _stateMachine.GetParent<EnemyChar>();
    _pathFollow = _parentChar.CurPathFollow;
  }

  internal override void Enter()
  {
    _parentChar.JustHit += ToCoverState;
    _movementController.SpeedMode = MovementController.SpeedType.Walk;
    _movementController.ControlMode = MovementController.ControlType.VelocityDirection;

    if (_pathFollow is null)
      return;

    Path3D path = _pathFollow.GetParent<Path3D>();
    _pathFollow.Progress = path.Curve.GetClosestOffset(path.ToLocal(_parentChar.GlobalPosition));
  }

  internal override void PhysicsProcess(double delta)
  {
    if (_pathFollow is null)
      return;
    
    _navAgent.TargetPosition = _pathFollow.GlobalPosition;

    if (!_navAgent.IsTargetReachable())
      return;

    Vector3 difVector = _navAgent.GetNextPathPosition() with { Y = 0f } - _parentChar.GlobalPosition;

    if (difVector.LengthSquared() < .01f)
    {
      _stateMachine.Transition(_patrolState);
      return;
    }

    _movementController.Direction = difVector.Normalized();
  }

  private void ToCoverState() => _stateMachine.Transition(_coverState);
}
