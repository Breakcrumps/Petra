using Godot;
using Petra.Static;

namespace Petra.Ui.DebugGraphics;

internal sealed partial class FogToggleBtn : CheckButton
{
  public override void _Ready()
  {
    ButtonPressed = GlobalInstances.CurrentEnv.Environment.FogEnabled;
    Toggled += toggledOn => GlobalInstances.CurrentEnv.Environment.FogEnabled = toggledOn;
  }
}
