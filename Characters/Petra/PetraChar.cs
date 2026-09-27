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

  public override void _EnterTree()
    => GlobalInstances.Petra = this;

  public override void _Ready()
  {
    _health = _maxHealth;
    _climbTargetRaycast.Enabled = false;
  }
  
  public override void _PhysicsProcess(double delta)
  {
    if (CurrentState == PetraState.Climbing)
      return;
    
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
    
    if (
      _hipsClimbCast.IsColliding() && !_chestClimbCast.IsColliding()
      && Input.IsActionPressed("Up") && Input.IsActionJustPressed("Jump")
    )
    {
      TryEnterClimbingState();
      return;
    }
    
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

  private void TryEnterClimbingState()
  {
    Vector3 raycastSrc = _hipsClimbCast.GetCollisionPoint() - (_camera.Basis.Z with { Y = 0f }).Normalized() * .2f;
    raycastSrc.Y += _chestClimbCast.Position.Y;

    _climbTargetRaycast.Enabled = true;
    _climbTargetRaycast.GlobalPosition = raycastSrc;
    _climbTargetRaycast.ForceRaycastUpdate();

    if (!_climbTargetRaycast.IsColliding())
      return;

    Vector3 targetPos = _climbTargetRaycast.GetCollisionPoint();
    _climbTargetRaycast.Enabled = false;

    float deltaY = targetPos.Y - GlobalPosition.Y;

    if (deltaY <= .05f)
      return;
    
    float runMult = CurrentState is PetraState.Running ? .8f : 1f;
    float deltaYMult = Mathf.Clamp(deltaY, .5f, 1.5f);
    float climbDuration = _climbDuration * deltaYMult * runMult;

    LastState = CurrentState;
    CurrentState = PetraState.Climbing;

    Tween tween = (
      CreateTween()
      .SetTrans(climbDuration < .12f ? Tween.TransitionType.Linear : Tween.TransitionType.Quad)
      .SetEase(climbDuration < .2f ? Tween.EaseType.Out : Tween.EaseType.InOut)
      .SetProcessMode(Tween.TweenProcessMode.Physics)
    );
    if (IsOnFloor())
      tween.TweenProperty(this, "global_position:y", GlobalPosition.Y - .05f * runMult, .03f * runMult * deltaYMult).SetEase(Tween.EaseType.In);
    tween.TweenInterval(.03f * runMult);
    tween.TweenProperty(this, "global_position", targetPos, climbDuration);
    tween.Parallel().TweenProperty(_camera.GunCamera, "global_position", targetPos + _camera.Position, climbDuration);
    tween.TweenCallback(Callable.From(() => CurrentState = LastState));
  }

  // private void EnterClimbingState()
  // {
  //   Vector3 raycastSrc = _hipsClimbCast.GetCollisionPoint() - (_camera.Basis.Z with { Y = 0f }).Normalized() * .5f;
  //   raycastSrc.Y += _chestClimbCast.Position.Y;

  //   _climbTargetRaycast.Enabled = true;
  //   _climbTargetRaycast.GlobalPosition = raycastSrc;
  //   _climbTargetRaycast.ForceRaycastUpdate();

  //   Vector3 targetPos = _climbTargetRaycast.GetCollisionPoint();
  //   _climbTargetRaycast.Enabled = false;

  //   LastState = CurrentState;
  //   CurrentState = PetraState.Climbing;

  //   Tween tween = CreateTween();

  //   Vector3 difVector = targetPos - GlobalPosition;
  //   float dist = difVector.Length();
  //   Vector3 flatDif = new(difVector.X, 0f, difVector.Z);
  //   float heightDif = targetPos.Y - GlobalPosition.Y;
  //   float duration = (flatDif.Length() + heightDif * 2f) / 10f;

  //   float exitMomentumFactor = Mathf.Lerp(1f, .1f, Mathf.InverseLerp(.5f, 2f, heightDif));

  //   Vector3 v1 = Velocity * .5f;
  //   v1.Y += heightDif;
  //   Vector3 v2 = Velocity * exitMomentumFactor;
  //   Vector3 p1 = GlobalPosition;

  //   GD.Print($"p1: {p1},\nv1: {v1},\np2: {targetPos},\nv2: {v2}");

  //   tween.TweenMethod(Callable.From<float>((t) =>
  //   {
  //     GlobalPosition = (
  //       (2 * t * t * t - 3 * t * t + 1) * p1
  //       + (t * t * t - 2 * t * t + t) * v1
  //       + (-2f * t * t * t + 3 * t * t) * targetPos
  //       + (t * t * t - t * t) * v2
  //     );
  //   }), 0f, 1f, duration);
  //   // _camera.Climb(duration, heightDif);
  //   tween.TweenCallback(Callable.From(() => CurrentState = LastState));
  // }

  // private void EnterClimbingState()
  // {
  //   Vector3 raycastSrc = _hipsClimbCast.GetCollisionPoint() - (_camera.Basis.Z with { Y = 0f }).Normalized() * .5f;
  //   raycastSrc.Y += _chestClimbCast.Position.Y;
  //   _climbTargetRaycast.Enabled = true;
  //   _climbTargetRaycast.GlobalPosition = raycastSrc;
  //   _climbTargetRaycast.ForceRaycastUpdate();
  //   Vector3 targetPos = _climbTargetRaycast.GetCollisionPoint();
  //   _climbTargetRaycast.Enabled = false;
  //   float deltaY = targetPos.Y - GlobalPosition.Y;
  //   float duration = _climbDuration * deltaY;
  //   LastState = CurrentState;
  //   if (CurrentState is PetraState.Running)
  //     duration *= .5f;
  //   CurrentState = PetraState.Climbing;
  //   Tween tween = (
  //     CreateTween().SetTrans(Tween.TransitionType.Sine)
  //     .SetEase(Tween.EaseType.In)
  //     .SetProcessMode(Tween.TweenProcessMode.Physics)
  //   );
  //   tween.TweenProperty(this, "global_position", targetPos, duration);
  //   _camera.Climb(duration, deltaY);
  //   tween.TweenCallback(Callable.From(() => CurrentState = LastState));
  // }

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
