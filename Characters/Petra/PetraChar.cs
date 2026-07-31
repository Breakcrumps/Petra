using Godot;
using Petra.Characters.Petra.Components;
using Petra.Objects.Guns.Scripts;
using Petra.Static;
using Petra.Types;

namespace Petra.Characters.Petra;

internal sealed partial class PetraChar : CharacterBody3D, IDamageable
{
  internal enum PetraState { Idle, Running, Crouching, Sliding, Climbing }
  internal PetraState CurrentState;

  [Export] private PetraCamera _camera = null!;
  [Export] private RayCast3D _slideCast = null!;
  [Export] private RayCast3D _hipsClimbCast = null!;
  [Export] private RayCast3D _feetClimbCast = null!;
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
  private bool _climbedThisJump;

  public override void _EnterTree()
    => GlobalInstances.Petra = this;

  public override void _Ready()
    => _health = _maxHealth;
  
  public override void _PhysicsProcess(double delta)
  {
    if (CurrentState == PetraState.Climbing)
      return;

    if (IsOnFloor())
      _climbedThisJump = false;
    
    _feetClimbCast.TargetPosition = -_camera.Basis.Z with { Y = 0f };
    _hipsClimbCast.TargetPosition = -_camera.Basis.Z with { Y = 0f };

    _feetClimbCast.ForceRaycastUpdate();
    _hipsClimbCast.ForceRaycastUpdate();
    
    if (
      !_climbedThisJump && !IsOnFloor()
      && !_hipsClimbCast.IsColliding() && _feetClimbCast.IsColliding()
      && Velocity.Y > -5f && Velocity.Y < 5f
      && (Velocity with { Y = 0f }).Normalized().Dot(-GlobalBasis.Z) > Mathf.Sqrt2 / 2f
      && Input.IsActionPressed("Up")
    )
      EnterClimbingState();
    else
      CurrentState = GetState();

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
      ) * .1f;
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

  private void EnterClimbingState()
  {
    _climbedThisJump = true;
    CurrentState = PetraState.Climbing;
    Velocity = Vector3.Zero;
    Vector3 startPos = GlobalPosition;
    Vector3 midPos = startPos with { Y = startPos.Y + .5f };
    Vector3 targetPos = _feetClimbCast.GetCollisionPoint();
    Vector3 lastPos = startPos, velocity = Vector3.Zero;
    targetPos.Y += .5f;
    Tween tween = (
      CreateTween().SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.In)
      .SetProcessMode(Tween.TweenProcessMode.Physics)
    );
    tween.TweenMethod(Callable.From<float>(t => 
    {
      float u = 1 - t;
      Vector3 newPos = u * u * startPos + 2f * u * t * midPos + t * t * targetPos;
      velocity = (newPos - lastPos) * Engine.PhysicsTicksPerSecond;
      GlobalPosition = lastPos = newPos;
    }), 0f, 1f, _climbDuration);
    tween.TweenCallback(Callable.From(() =>
    {
      CurrentState = PetraState.Idle;
      Velocity = velocity;
    }));
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

  private PetraState GetState() => CurrentState switch
  {
    PetraState.Idle => HandleIdleTransitions(),
    PetraState.Running => HandleRunningTransitions(),
    PetraState.Crouching => HandleCrouchTransitions(),
    PetraState.Sliding => HandleSlideTransitions(),
    _ => PetraState.Idle
  };

  private PetraState HandleIdleTransitions()
  {
    if (Input.IsActionPressed("Run") && Velocity != Vector3.Zero)
      return PetraState.Running;
    else if (Input.IsActionJustPressed("Crouch"))
      return PetraState.Crouching;
    return PetraState.Idle;
  }


  private PetraState HandleCrouchTransitions()
  {
    if (Input.IsActionPressed("Run") && Velocity != Vector3.Zero)
      return PetraState.Running;
    else if (Input.IsActionJustPressed("Crouch"))
      return PetraState.Idle;
    return PetraState.Crouching;
  }


  private PetraState HandleRunningTransitions()
  {
    if (Input.IsActionJustPressed("Crouch"))
    {
      if (Input.IsActionPressed("Up") && IsOnFloor() && Velocity != Vector3.Zero)
        return InitSlide();
      if (Velocity == Vector3.Zero)
        return PetraState.Crouching;
    }
    if (!Input.IsActionPressed("Run") || Velocity == Vector3.Zero)
      return PetraState.Idle;
    return PetraState.Running;
  }

  private static PetraState HandleSlideTransitions()
  {
    if (Input.IsActionPressed("Down"))
      return PetraState.Crouching;
    else if (Input.IsActionJustPressed("Jump"))
      return PetraState.Idle;
    return PetraState.Sliding;
  }

  private PetraState InitSlide()
  {
    _slideDirection = Velocity.Normalized() with { Y = 0f };
    _slideCast.TargetPosition = _slideDirection;
    _slideCast.ForceRaycastUpdate();
    _slideTimer = _slideTime;
    return PetraState.Sliding;
  }

  public void TakeDamage(Attack attack)
  {
    _health -= attack.Damage;

    if (_health <= 0f)
      GD.Print("You Died!");
  }
}
