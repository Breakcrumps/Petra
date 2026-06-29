using Godot;

namespace Petra.Utils.BulletReactors;

internal sealed partial class BreakableStaticBody : StaticBody3D, IBreakable
{
  [Export] public PackedScene BrokenScene { get; private set; } = null!;

  private Breaker _breaker;
  public Breaker Breaker => _breaker;

  public override void _Ready()
    => _breaker = new Breaker(this, BrokenScene, GetParent<NavigationRegion3D>());
}
