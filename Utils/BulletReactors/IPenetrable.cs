namespace Petra.Utils.BulletReactors;

internal interface IPenetrable
{
  int DamageReduction { get; }
  float ImpulseFromBulletCoefficient { get; }
}
