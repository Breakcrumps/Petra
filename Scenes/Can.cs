using Godot;

internal sealed partial class Can : RigidBody3D, IPenetrable
{
  public int DamageReduction => 10;
}
