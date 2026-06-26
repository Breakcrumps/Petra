using Godot;

namespace Petra.Characters.Petra.Components;

[GlobalClass]
internal sealed partial class PetraGunCamera : Camera3D
{
  [Export] private PetraCamera _mainCamera = null!;
  
  public override void _PhysicsProcess(double delta)
  {
    GlobalPosition = _mainCamera.GlobalPosition;
    Rotation = _mainCamera.Rotation;
  }
}
