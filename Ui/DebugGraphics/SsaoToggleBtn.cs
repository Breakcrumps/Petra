using Godot;
using Petra.Static;

namespace Petra.Ui.DebugGraphics;

internal sealed partial class SsaoToggleBtn : CheckButton
{
  public override void _Ready()
  {
    ButtonPressed = GlobalInstances.CurrentEnv.Environment.SsaoEnabled;
    Toggled += toggledOn => GlobalInstances.CurrentEnv.Environment.SsaoEnabled = toggledOn;
  }
}
