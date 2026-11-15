using Godot;

namespace Petra.Utils.BulletReactors;

internal sealed partial class BreakablePenetrableStaticBody : StaticBody3D, IPenetrable, IBreakable
{
  [Export] private int _damageReduction = 2;
  public int GetDamageReduction() => _damageReduction;

  [Export] private float _impulseFromBulletCoefficient = .95f;
  public float GetImpulseFromBulletCoefficient() => _impulseFromBulletCoefficient;

  [Export] private PackedScene _brokenScene = null!;
  public PackedScene GetBrokenScene() => _brokenScene;

  private Breaker _breaker;
  public Breaker GetBreaker() => _breaker;

  public override void _Ready()
    => _breaker = new Breaker(this, _brokenScene, GetParent<NavigationRegion3D>());
}
