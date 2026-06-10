using Godot;

namespace Petra.Utils;

internal sealed partial class BreakablePenetrableStaticBody : StaticBody3D, IPenetrable, IBreakable
{
  [Export] public int DamageReduction { get; private set; } = 10;
  [Export] public PackedScene BrokenScene { get; private set; } = null!;

  private Breaker _breaker;
  public Breaker Breaker => _breaker;

  public override void _Ready()
    => _breaker = new Breaker(this, BrokenScene, GetParent<NavigationRegion3D>());
}
