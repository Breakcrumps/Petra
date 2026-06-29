using Godot;
using Petra.Static;
using Petra.Utils.Cover;

namespace Petra.Characters.Enemy;

internal sealed partial class CoverState : State
{
  [Export] private MovementController _movementController = null!;
  [Export] private NavigationAgent3D _navAgent = null!;

  private EnemyChar _parentChar = null!;

  public override void _Ready()
    => _parentChar = GetParent().GetParent<EnemyChar>();
  
  internal override void Enter()
  {
    _movementController.ControlMode = MovementController.ControlType.TeleportDirection;
    _movementController.SpeedMode = MovementController.SpeedType.Run;
  }

  internal override void PhysicsProcess(double delta)
  {
    CoverMarker? bestCover = GlobalInstances.CoverManager.GetBestCover(_parentChar, GlobalInstances.Petra);

    if (bestCover is null)
      return;

    _navAgent.TargetPosition = bestCover.GlobalPosition;

    if (!_navAgent.IsTargetReachable())
      return;

    Vector3 difVector = _navAgent.GetNextPathPosition() with { Y = 0f } - _parentChar.GlobalPosition;

    if (difVector.LengthSquared() < .01f)
    {
      _movementController.Direction = Vector3.Zero;
      _parentChar.Quaternion =_parentChar.Quaternion.Slerp(
        Basis.LookingAt(_parentChar.GlobalPosition - bestCover.Basis.Z).GetRotationQuaternion(),
        10f * (float)delta
      );
      return;
    }

    _movementController.Direction = difVector.Normalized();
  }
}
