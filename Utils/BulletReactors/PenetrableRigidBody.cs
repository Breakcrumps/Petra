using Godot;

namespace Petra.Utils.BulletReactors;

internal sealed partial class PenetrableRigidBody : RigidBody3D, IPenetrable
{
  [Export] private int _damageReduction = 10;
  public int GetDamageReduction() => _damageReduction;

  [Export] private float _impulseFromBulletCoefficient = .1f;
  public float GetImpulseFromBulletCoefficient() => _impulseFromBulletCoefficient;
}
