using Godot;

namespace Petra.Utils;

[GlobalClass]
internal sealed partial class GameFlowController : Node
{
  public override void _Ready()
    => ProcessMode = ProcessModeEnum.Always;

  public override void _PhysicsProcess(double delta)
  {
    bool paused = GetTree().Paused;
    if (Input.IsActionJustPressed("Pause"))
    {
      paused = !paused;
      GetTree().Paused = paused;
    }
    Input.MouseMode = paused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
  }
}
