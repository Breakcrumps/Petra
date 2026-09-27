using Godot;

namespace Petra.Levels.TestLevel;

[GlobalClass]
internal sealed partial class SlowDownTime : Node
{
  public override void _PhysicsProcess(double delta)
  {
    if (Input.IsActionJustPressed("SlowDownTime"))
      Engine.TimeScale = Engine.TimeScale == 1f ? .1f : 1f;
  }
}
