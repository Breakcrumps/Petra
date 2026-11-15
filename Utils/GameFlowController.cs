using Godot;
using Petra.Static;

namespace Petra.Utils;

[GlobalClass]
internal sealed partial class GameFlowController : Node
{
  public override void _Ready()
    => ProcessMode = ProcessModeEnum.Always;

  public override void _PhysicsProcess(double delta)
  {
    if (GlobalInstances.Petra.CurrentState is Characters.Petra.PetraChar.PetraState.Dead)
      return;
    bool paused = GetTree().Paused;
    if (Input.IsActionJustPressed("Pause"))
    {
      paused = !paused;
      GetTree().Paused = paused;
    }
    Input.MouseMode = paused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
  }
}
