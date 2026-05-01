using Godot;
using Petra.Static;

namespace Petra.Ui.DebugGraphics;

internal sealed partial class ToneMapOptionMenu : OptionButton
{
  public override void _Ready()
  {
    Selected = GlobalInstances.CurrentEnv.Environment.TonemapMode switch
    {
      Environment.ToneMapper.Aces => 1,
      _ => 0
    };
    
    ItemSelected += idx => GlobalInstances.CurrentEnv.Environment.TonemapMode = idx switch
    {
      1 => Environment.ToneMapper.Aces,
      _ => Environment.ToneMapper.Linear
    };
  }
}
