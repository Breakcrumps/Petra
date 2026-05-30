using Godot;

namespace Petra.Characters.Petra.Components;

internal sealed partial class PetraCollision : CollisionShape3D
{
  [Export] private PetraChar _petra = null!;
  [Export] private Node3D _crouchPivot = null!;
  [Export] private Node3D _slidePivot = null!;
  
  private float _initHeight;

  public override void _Ready()
    => _initHeight = ((CapsuleShape3D)Shape).Height;
  
  public override void _PhysicsProcess(double delta)
  {
    switch (_petra.CurrentState)
    {
      case PetraChar.PetraState.Sliding:
        SetCollisionHeight(_slidePivot.Position.Y);
        break;
      case PetraChar.PetraState.Crouching:
        SetCollisionHeight(_crouchPivot.Position.Y);
        break;
      default:
        SetCollisionHeight(_initHeight);
        break;
    }
  }

  private void SetCollisionHeight(float newHeight)
  {
    ((CapsuleShape3D)Shape).Height = newHeight;
    Position = Position with { Y = newHeight / 2f };
  }
}
