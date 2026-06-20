using Godot;

namespace Petra.Characters.Petra.Components;

[GlobalClass]
internal sealed partial class PetraGunCamera : Camera3D
{
  [Export] private PetraCamera _mainCamera = null!;
  [Export] internal float XOffset;
  
  public override void _PhysicsProcess(double delta)
  {
    GlobalPosition = _mainCamera.GlobalPosition + _mainCamera.Basis.X * XOffset;
    Rotation = _mainCamera.Rotation;
  }
}
