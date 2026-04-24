using Godot;
using Petra.Characters.Petra.Components;
using Petra.Types;

namespace Petra.Characters.Petra;

internal sealed partial class PetraChar : CharacterBody3D, IDamageable
{
  internal enum PetraState { Idle, Running, Crouching }
  internal PetraState CurrentState { get; private set; }

  [Export] private PetraCamera _camera = null!;
  
  [Export] private int _maxHealth = 100;
  private int _health;
  
  [Export] private float _walkSpeed = 5f;
  [Export] private float _runSpeed = 9f;
  [Export] private float _crouchSpeed = 3f;

  [Export] private float _jumpVelocity = 10f;
  [Export] private float _gravity = 30f;

  internal float TimeMoving { get; private set; }

  [Export] private float _jumpBufferTime = .1f;
  private float _jumpBufferCounter;

  public override void _Ready()
    => _health = _maxHealth;
  
  public override void _PhysicsProcess(double delta)
  {
    CurrentState = GetPetraState();
    
    float speed = CurrentState switch
    {
      PetraState.Idle => _walkSpeed,
      PetraState.Running => _runSpeed,
      PetraState.Crouching => _crouchSpeed,
      _ => _walkSpeed
    };

    Vector2 floorVelocity = Input.GetVector(
      negativeX: "Left", positiveX: "Right",
      negativeY: "Down", positiveY: "Up"
    ) * speed;
    floorVelocity = floorVelocity.Rotated(_camera.Rotation.Y);

    float yVelocity = Velocity.Y;
    if (!IsOnFloor())
      yVelocity -= _gravity * (float)delta;

    if (Input.IsActionJustPressed("Jump"))
      _jumpBufferCounter = _jumpBufferTime;
    else
      _jumpBufferCounter = Mathf.Max(_jumpBufferCounter - (float)delta, 0f);

    if (_jumpBufferCounter > 0f && IsOnFloor())
    {
      yVelocity = _jumpVelocity;
      _jumpBufferCounter = 0f;
    }

    Velocity = new Vector3(floorVelocity.X, yVelocity, -floorVelocity.Y);
    MoveAndSlide();
    
    if (IsOnFloor() && (Velocity.X, Velocity.Z) != (0f, 0f))
      TimeMoving += (float)delta;
    else
      TimeMoving = 0f;
  }

  private PetraState GetPetraState()
  {
    if (Input.IsActionPressed("Run") && Velocity != Vector3.Zero)
      return PetraState.Running;
    else if (Input.IsActionJustPressed("Crouch"))
      return CurrentState == PetraState.Crouching ? PetraState.Idle : PetraState.Crouching;
    else if (CurrentState == PetraState.Crouching)
      return PetraState.Crouching;
    return PetraState.Idle;
  }

  public void TakeDamage(Attack attack)
  {
    _health -= attack.Damage;

    if (_health <= 0)
      GD.Print("You died.");
  }
}
