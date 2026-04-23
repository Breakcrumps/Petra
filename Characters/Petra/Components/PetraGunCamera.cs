using Godot;

namespace Petra.Characters.Petra.Components;

internal sealed partial class PetraGunCamera : Camera3D
{
  [Export] private PetraCamera _mainCamera = null!;
  
  public override void _PhysicsProcess(double delta)
    => Rotation = _mainCamera.Rotation;
}
