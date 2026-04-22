using Godot;

internal sealed partial class PetraGunCamera : Camera3D
{
  [Export] private PetraCamera _mainCamera = null!;
  
  public override void _PhysicsProcess(double delta)
    => Rotation = _mainCamera.Rotation;
}
