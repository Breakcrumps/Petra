using Godot;

namespace Petra.Characters.Enemy;

[GlobalClass]
internal sealed partial class MovementController : Node
{
  internal enum ControlType { Position, Velocity, Direction, TeleportDirection }
  internal ControlType ControlMode = ControlType.Position;

  internal enum SpeedType { Walk, Run }
  internal SpeedType SpeedMode = SpeedType.Walk;

  [Export] private CharacterBody3D _char = null!;
  [Export] private float _walkSpeed = 1f;
  [Export] private float _runSpeed = 3f;

  internal Vector3 NextPosition;
  internal Vector3 Velocity;
  internal Vector3 Direction;

  internal Vector2 GroundMoveDirLocal;

  public override void _Ready()
    => NextPosition = _char.GlobalPosition;

  public override void _PhysicsProcess(double delta)
  {
    Vector3 lookDir = Vector3.Forward;
    float speed = SpeedMode == SpeedType.Walk ? _walkSpeed : _runSpeed;
    
    switch (ControlMode)
    {
      case ControlType.Position:
        lookDir = (NextPosition - _char.GlobalPosition).Normalized();
        _char.GlobalPosition = NextPosition;
        break;
      case ControlType.Velocity:
        _char.Velocity = Velocity;
        _char.MoveAndSlide();
        lookDir = Velocity;
        break;
      case ControlType.Direction:
        Velocity = Direction * speed;
        goto case ControlType.Velocity;
      case ControlType.TeleportDirection:
        NextPosition = _char.GlobalPosition + Direction * speed * (float)delta;
        goto case ControlType.Position;
    }

    if (lookDir == Vector3.Zero)
      return;

    _char.Quaternion = _char.Quaternion.Slerp(Basis.LookingAt(lookDir).GetRotationQuaternion(), 10f * (float)delta);
    lookDir = _char.GlobalBasis.Inverse() * lookDir;
    GroundMoveDirLocal = new Vector2(lookDir.X, -lookDir.Z).Normalized();
  }
}
