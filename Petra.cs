using Godot;

internal sealed partial class Petra : CharacterBody3D, IDamageable
{
  internal enum PetraState { Idle, Running, Crouching }
  internal PetraState CurrentState { get; private set; }

  [Export] private PetraCamera _camera = null!;
  
  [Export] private int _maxHealth = 100;
  private int _health;
  
  [Export] private float _walkSpeed = 10f;
  [Export] private float _runSpeed = 20f;
  [Export] private float _crouchSpeed = 5f;

  [Export] private float _jumpVelocity = 10f;
  [Export] private float _gravity = 30f;

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

    if (IsOnFloor() && Input.IsActionJustPressed("Jump"))
      yVelocity = _jumpVelocity;

    Velocity = new Vector3(floorVelocity.X, yVelocity, -floorVelocity.Y);
    MoveAndSlide();
  }

  private static PetraState GetPetraState() => (
    Input.IsActionPressed("Crouch")
    ? PetraState.Crouching
    : Input.IsActionPressed("Run")
    ? PetraState.Running
    : PetraState.Idle
  );

  public void TakeDamage(Attack attack)
  {
    _health -= attack.Damage;

    if (_health <= 0)
      GD.Print("You died.");
  }
}
