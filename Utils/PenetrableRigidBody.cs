using Godot;

namespace Petra.Utils;

internal sealed partial class PenetrableRigidBody : RigidBody3D, IPenetrable
{
  [Export] public int DamageReduction { get; private set; } = 10;
  [Export] public float ImpulseFromBulletCoefficient { get; private set; } = .1f;
}
