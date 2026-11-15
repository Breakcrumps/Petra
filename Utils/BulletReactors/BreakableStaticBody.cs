using Godot;

namespace Petra.Utils.BulletReactors;

internal sealed partial class BreakableStaticBody : StaticBody3D, IBreakable
{
  [Export] private PackedScene _brokenScene = null!;
  public PackedScene GetBrokenScene() => _brokenScene;

  private Breaker _breaker;
  public Breaker GetBreaker() => _breaker;

  public override void _Ready()
    => _breaker = new Breaker(this, _brokenScene, GetParent<NavigationRegion3D>());
}
