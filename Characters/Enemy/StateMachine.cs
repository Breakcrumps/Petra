using Godot;

namespace Petra.Characters.Enemy;

[GlobalClass]
internal sealed partial class StateMachine : Node
{
  [Export] private State _initState = null!;
  internal State CurState = null!;

  public override void _Ready()
  {
    CurState = _initState;
    CurState.Enter();
  }

  public override void _Process(double delta)
    => CurState.Process(delta);
  public override void _PhysicsProcess(double delta)
    => CurState.PhysicsProcess(delta);

  internal void Transition(State newState)
  {
    CurState.Exit();
    CurState = newState;
    newState.Enter();
  }
}
