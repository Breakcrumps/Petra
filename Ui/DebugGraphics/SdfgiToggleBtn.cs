using Godot;
using Petra.Static;

namespace Petra.Ui.DebugGraphics;

internal sealed partial class SdfgiToggleBtn : CheckButton
{
  public override void _Ready()
  {
    ButtonPressed = GlobalInstances.CurrentEnv.Environment.SdfgiEnabled;
    Toggled += toggledOn => GlobalInstances.CurrentEnv.Environment.SdfgiEnabled = toggledOn;
  }
}
