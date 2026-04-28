using Godot;

namespace Petra.GlobalNodes;

[GlobalClass]
internal sealed partial class GameFlowController : Node
{
  public override void _Ready()
    => ProcessMode = ProcessModeEnum.Always;

  public override void _PhysicsProcess(double delta)
  {
    if (Input.IsActionJustPressed("Pause"))
    {
      bool paused = GetTree().Paused;
      GetTree().Paused = !paused;
      Input.MouseMode = paused ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
    }
  }
}
