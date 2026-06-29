using Godot;

namespace Petra.Characters.Enemy;

internal sealed partial class PatrolState : State
{
  [Export] private float _patrolSpeed = 1f;
  [Export] private MovementController _movementController = null!;

  [ExportGroup("TransitableStates")]
  [Export] private CoverState _coverState = null!;

  private EnemyChar _enemyChar = null!;
  private PathFollow3D? _pathFollow;
  private StateMachine _stateMachine = null!;

  public override void _Ready()
  {
    _stateMachine = GetParent<StateMachine>();
    _enemyChar = _stateMachine.GetParent<EnemyChar>();
    _pathFollow = _enemyChar.CurPathFollow;
  }

  internal override void Enter()
  {
    _enemyChar.JustHit += ToCoverState;
    _movementController.ControlMode = MovementController.ControlType.Position;
    _movementController.SpeedMode = MovementController.SpeedType.Walk;
  }

  internal override void Exit()
    => _enemyChar.JustHit -= ToCoverState;

  internal override void PhysicsProcess(double delta)
  {
    if (_pathFollow is not null)
    {
      _pathFollow.Progress += _patrolSpeed * (float)delta;
      _movementController.NextPosition = _pathFollow.GlobalPosition;
    }
  }

  private void ToCoverState() => _stateMachine.Transition(_coverState);
}
