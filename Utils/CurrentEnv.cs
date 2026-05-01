using Godot;
using Petra.Static;

namespace Petra.Utils;

internal sealed partial class CurrentEnv : WorldEnvironment
{
  public override void _Ready()
    => GlobalInstances.CurrentEnv = this;
} 
