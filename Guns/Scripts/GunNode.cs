using Godot;

namespace Petra.Objects.Guns.Scripts;

internal sealed partial class GunNode : Node3D
{
  [Export] private PackedScene _shellScene = null!;
  [Export] private Node3D _shellEjectPivot = null!;
  [Export] internal GunData GunData = null!;

  [Export] internal AnimationPlayer AnimPlayer = null!;

  internal int CartridgesChambered;
  internal int CartridgesInMag;

  private void EjectShell()
  {
    RigidBody3D shell = _shellScene.Instantiate<RigidBody3D>();
    GetTree().CurrentScene.AddChild(shell);
    shell.GlobalTransform = _shellEjectPivot.GlobalTransform;
    shell.ApplyCentralImpulse(_shellEjectPivot.GlobalBasis.X * .05f);
    shell.ApplyTorqueImpulse(_shellEjectPivot.GlobalBasis.X * (float)GD.RandRange(-2.0, 2.0));
    shell.ApplyTorqueImpulse(_shellEjectPivot.GlobalBasis.Y * (float)GD.RandRange(-2.0, 2.0));
    shell.ApplyTorqueImpulse(_shellEjectPivot.GlobalBasis.Z * (float)GD.RandRange(-2.5, 2.5));
  }

  internal void RefillAmmo()
    => CartridgesInMag = GunData.CartridgesInMag;

  internal void Chamber()
  {
    CartridgesChambered = GunData.CartridgesChambered;
    CartridgesInMag -= GunData.CartridgesChambered;
  }
}
