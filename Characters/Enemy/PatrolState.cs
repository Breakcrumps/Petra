using Godot;

namespace Petra.Characters.Enemy;

[GlobalClass]
internal sealed partial class PatrolState : State
{
  [Export] private float _patrolSpeed = 1f;
  [Export] private MovementController _movementController = null!;

  [ExportGroup("TransitableStates")]
  [Export] private CoverState _coverState = null!;

  private EnemyChar _parentChar = null!;
  private PathFollow3D? _pathFollow;
  private StateMachine _stateMachine = null!;

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
    _movementController.ControlMode = MovementController.ControlType.Position;
  }

  internal override void Exit()
    => _parentChar.JustHit -= ToCoverState;

  internal override void PhysicsProcess(double delta)
  {
    if (_pathFollow is null)
      return;

    _pathFollow.Progress += _patrolSpeed * (float)delta;
    _movementController.NextPosition = _pathFollow.GlobalPosition;
    _parentChar.Quaternion = _parentChar.Quaternion.Slerp(
      Basis.LookingAt((_pathFollow.GlobalPosition - _parentChar.GlobalPosition) with { Y = 0f }).GetRotationQuaternion(),
      10f * (float)delta
    );
  }

  // For bullet hits.
  private void ToCoverState() => _stateMachine.Transition(_coverState);
}
