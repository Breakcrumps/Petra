using Godot;

namespace Petra.Utils;

internal sealed partial class BreakableStaticBody : StaticBody3D, IBreakable
{
  [Export] public PackedScene BrokenScene { get; private set; } = null!;
  public Breaker Breaker => new(this, BrokenScene);
}
