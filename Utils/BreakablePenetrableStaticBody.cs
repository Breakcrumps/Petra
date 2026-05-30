using Godot;

namespace Petra.Utils;

internal sealed partial class BreakablePenetrableStaticBody : StaticBody3D, IPenetrable, IBreakable
{
  [Export] public int DamageReduction { get; private set; } = 10;
  [Export] public PackedScene BrokenScene { get; private set; } = null!;
  public Breaker Breaker => new(this, BrokenScene);
}
