using Godot;

namespace Petra.Characters.Enemy;

internal sealed partial class EnemyStateMachine : Node
{
  [Export] private State _initState = null!;
  private State _curState = null!;

  public override void _Ready()
    => _curState = _initState;

  public override void _Process(double delta)
    => _curState.Process(delta);
  public override void _PhysicsProcess(double delta)
    => _curState.PhysicsProcess(delta);

  internal void Transition(State newState)
  {
    _curState.Exit();
    _curState = newState;
    newState.Enter();
  }
}
