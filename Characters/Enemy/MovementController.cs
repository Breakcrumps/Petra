using Godot;

namespace Petra.Characters.Enemy;

[GlobalClass]
internal sealed partial class MovementController : Node
{
  internal enum ControlType { Position, Velocity, VelocityDirection, PositionDirection }
  internal ControlType ControlMode = ControlType.Position;

  internal enum SpeedType { Walk, Run }
  internal SpeedType SpeedMode = SpeedType.Walk;

  [Export] private CharacterBody3D _char = null!;
  [Export] private float _walkSpeed = 1f;
  [Export] private float _runSpeed = 2f;

  internal Vector3 NextPosition;
  internal Vector3 Velocity;
  internal Vector3 Direction;

  internal Vector2 GroundMoveDirLocal;

  public override void _Ready()
    => NextPosition = _char.GlobalPosition;

  public override void _PhysicsProcess(double delta)
  {
    Vector3 moveDir = Vector3.Forward;
    float speed = SpeedMode == SpeedType.Walk ? _walkSpeed : _runSpeed;
    
    switch (ControlMode)
    {
      case ControlType.Position:
        moveDir = (NextPosition - _char.GlobalPosition).Normalized();
        _char.GlobalPosition = NextPosition;
        break;
      case ControlType.Velocity:
        _char.Velocity = Velocity;
        _char.MoveAndSlide();
        moveDir = Velocity;
        break;
      case ControlType.VelocityDirection:
        Velocity = Direction * speed;
        goto case ControlType.Velocity;
      case ControlType.PositionDirection:
        NextPosition = _char.GlobalPosition + Direction * speed * (float)delta;
        goto case ControlType.Position;
    }

    moveDir = _char.GlobalBasis.Inverse() * moveDir;
    GroundMoveDirLocal = new Vector2(moveDir.X, -moveDir.Z).Normalized();
  }
}
