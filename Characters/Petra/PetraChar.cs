using Godot;
using Petra.Characters.Petra.Components;
using Petra.Objects.Guns.Scripts;
using Petra.Static;
using Petra.Types;

namespace Petra.Characters.Petra;

internal sealed partial class PetraChar : CharacterBody3D, IDamageable
{
  internal enum PetraState { Idle, Running, Crouching, Sliding }
  internal PetraState CurrentState;

  [Export] private PetraCamera _camera = null!;
  [Export] private RayCast3D _slideCast = null!;
  [Export] private RayCast3D _hipsClimbCast = null!;
  [Export] private RayCast3D _chestClimbCast = null!;
  [Export] private RayCast3D _climbTargetRaycast = null!;
  [Export] internal GunsWrapper Gun = null!;

  internal Marker3D TorsoPoint = null!;
  [Export] private Marker3D _standingTorsoPoint = null!;
  [Export] private Marker3D _crouchingTorsoPoint = null!;
  [Export] internal Marker3D HeadPoint = null!;

  [Export] private int _maxHealth = 100;
  private int _health;
  
  [Export] private float _walkSpeed = 5f;
  [Export] private float _runSpeed = 9f;
  [Export] private float _crouchSpeed = 3f;
  [Export] private float _climbDuration = .2f;

  [Export] private float _jumpVelocity = 8f;
  [Export] private float _gravity = 25f;

  internal float TimeMoving;

  [Export] private float _jumpBufferTime = .1f;
  private float _jumpBufferCounter;

  [Export] private float _slideTime = 1f;
  [Export] private float _slideSpeed = 7f;
  private Vector3 _slideDirection;
  private float _slideTimer;

  internal PetraState LastState;

  internal bool HighClimb;

  public override void _EnterTree()
    => GlobalInstances.Petra = this;

  public override void _Ready()
  {
    _health = _maxHealth;
    _climbTargetRaycast.Enabled = false;
  }
  
  public override void _PhysicsProcess(double delta)
  {
    _chestClimbCast.TargetPosition = -_camera.Basis.Z with { Y = 0f };
    _hipsClimbCast.TargetPosition = -_camera.Basis.Z with { Y = 0f };

    if (CurrentState == PetraState.Running)
    {
      _chestClimbCast.TargetPosition *= 1.5f;
      _hipsClimbCast.TargetPosition *= 1.5f;
    }

    _chestClimbCast.ForceRaycastUpdate();
    _hipsClimbCast.ForceRaycastUpdate();
    _chestClimbCast.ForceUpdateTransform();
    _hipsClimbCast.ForceUpdateTransform();
    
    UpdateState();

    if (CurrentState == PetraState.Crouching)
      TorsoPoint = _crouchingTorsoPoint;
    else
      TorsoPoint = _standingTorsoPoint;

    if (CurrentState == PetraState.Sliding)
      HandleSlide(delta);
    else
      HandleMovement(delta);
  }

  private void HandleMovement(double delta)
  {
    Vector2 curFloorVelocity = new(Velocity.X, -Velocity.Z);
    
    float speed = Mathf.Lerp(curFloorVelocity.Length(), CurrentState switch
    {
      PetraState.Idle => _walkSpeed,
      PetraState.Running => _runSpeed,
      PetraState.Crouching => _crouchSpeed,
      _ => _walkSpeed
    }, 8f * (float)delta);

    Vector2 floorVelocity;

    if (IsOnFloor())
    {
      floorVelocity = Input.GetVector(
        negativeX: "Left", positiveX: "Right",
        negativeY: "Down", positiveY: "Up"
      ) * speed;
      floorVelocity = floorVelocity.Rotated(_camera.Rotation.Y);
    }
    else
    {
      floorVelocity = curFloorVelocity.Lerp(Vector2.Zero, .7f * (float)delta);
      Vector2 inputOffset = Input.GetVector(
        negativeX: "Left", positiveX: "Right",
        negativeY: "Down", positiveY: "Up"
      ) * .2f;
      inputOffset = inputOffset.Rotated(_camera.Rotation.Y);
      if (inputOffset.Dot(curFloorVelocity) / (inputOffset.Length() * curFloorVelocity.Length()) > Mathf.Sqrt2 / 2f)
        inputOffset *= .1f;
      floorVelocity += inputOffset;
    }

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

  private void HandleSlide(double delta)
  {
    if (_slideCast.IsColliding() || !IsOnFloor())
    {
      CurrentState = PetraState.Crouching;
      return;
    }
    
    Vector3 newVelocity = _slideDirection * _slideSpeed;

    if (!IsOnFloor())
      newVelocity.Y = Velocity.Y - _gravity * (float)delta;
    
    Velocity = newVelocity;
    
    MoveAndSlide();

    _slideTimer -= (float)delta;

    if (_slideTimer <= 0f)
      CurrentState = PetraState.Crouching;
  }

  private void UpdateState()
  {
    switch (CurrentState)
    {
      case PetraState.Idle:
        HandleIdleTransitions();
        break;
      case PetraState.Running:
        HandleRunningTransitions();
        break;
      case PetraState.Crouching:
        HandleCrouchTransitions();
        break;
      case PetraState.Sliding:
        HandleSlideTransitions();
        break;
      default:
        CurrentState = PetraState.Idle;
        break;
    };
  }

  private void HandleIdleTransitions()
  {
    if (Input.IsActionPressed("Run") && Velocity != Vector3.Zero)
      CurrentState = PetraState.Running;
    else if (Input.IsActionJustPressed("Crouch"))
      CurrentState = PetraState.Crouching;
  }


  private void HandleCrouchTransitions()
  {
    if (Input.IsActionPressed("Run") && Velocity != Vector3.Zero)
      CurrentState = PetraState.Running;
    else if (Input.IsActionJustPressed("Crouch"))
      CurrentState = PetraState.Idle;
  }


  private void HandleRunningTransitions()
  {
    if (Input.IsActionJustPressed("Crouch"))
    {
      if (Input.IsActionPressed("Up") && IsOnFloor() && Velocity != Vector3.Zero)
        InitSlide();
      if (Velocity == Vector3.Zero)
        CurrentState = PetraState.Crouching;
    }
    if (!Input.IsActionPressed("Run") || Velocity == Vector3.Zero)
      CurrentState = PetraState.Idle;
  }

  private void HandleSlideTransitions()
  {
    if (Input.IsActionPressed("Down"))
      CurrentState = PetraState.Crouching;
    else if (Input.IsActionJustPressed("Jump"))
      CurrentState = PetraState.Idle;
  }

  private void InitSlide()
  {
    _slideDirection = Velocity.Normalized() with { Y = 0f };
    _slideCast.TargetPosition = _slideDirection;
    _slideCast.ForceRaycastUpdate();
    _slideTimer = _slideTime;
    CurrentState = PetraState.Sliding;
  }

  public void TakeDamage(Attack attack)
  {
    _health -= attack.Damage;

    if (_health <= 0f)
      GD.Print("You Died!");
  }
}
