using System;
using Godot;
using Petra.Characters.Petra.Components;
using Petra.Objects.Guns.Scripts;
using Petra.Static;
using Petra.Types;

namespace Petra.Characters.Petra;

internal sealed partial class PetraChar : CharacterBody3D, IDamageable
{
  internal event Action<int>? HealthChanged;
  internal event Action<float>? StaminaChanged;
  internal event Action? AdrenalineDeath;
  internal event Action? AdrenalineDeathAverted;
  internal event Action? Died;
  internal Action? Fallen;
  
  internal enum PetraState { Idle, Running, Crouching, Sliding, Dead }
  internal PetraState CurrentState;

  [Export] private PetraCamera _camera = null!;
  [Export] private RayCast3D _slideCast = null!;
  [Export] private RayCast3D _hipsClimbCast = null!;
  [Export] private RayCast3D _chestClimbCast = null!;
  [Export] internal GunsWrapper Gun = null!;
  [Export] private AudioStreamPlayer3D _gruntPlayer = null!;
  [Export] private RayCast3D _deathRaycast = null!;

  internal Marker3D TorsoPoint = null!;
  [Export] private Marker3D _standingTorsoPoint = null!;
  [Export] private Marker3D _crouchingTorsoPoint = null!;
  [Export] internal Marker3D HeadPoint = null!;

  [Export] private int _maxHealth = 100;
  internal int Health;
  [Export] private float _maxStamina = 100f;
  internal float Stamina;
  [Export] private float _staminaSpendRate = 25f;
  
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

  internal Vector3[] LastTorsoPos = new Vector3[120];
  internal Vector3[] LastHeadPos = new Vector3[120];

  private CollisionShape3D _collision = null!;
  private float _initCollisionY;
  private float _initCollisionHeight;

  [Export] private bool _adrenaline;
  [Export] private float _adrenalineDeathTime = 5f;
  private float _adrenalineDeathTimer;
  private bool _dyingAdrenaline;

  private bool _healViable = true;
  [Export] private float _healStartTime = 1f;
  private float _healTimer;
  [Export] private float _maxSecPerHeal = .5f, _minSecPerHeal = .05f;
  private float _timeHealing, _maxTimeHealing = 1.3f;

  [Export] private float _staminaHealStartTime = 1f;
  private float _staminaTimer;
  [Export] private float _staminaReplenishRate = 35f;

  public override void _EnterTree()
  {
    GlobalInstances.Petra = this;
    Health = _maxHealth;
    Stamina = _maxStamina;
  }

  public override void _Ready()
  {
    _collision = GetNode<CollisionShape3D>("CollisionShape3D");
    _initCollisionY = _collision.Position.Y;
    _initCollisionHeight = ((CapsuleShape3D)_collision.Shape).Height;
  }
  
  public override void _PhysicsProcess(double delta)
  {
    if (CurrentState is PetraState.Dead)
      return;
    
    _healTimer = Mathf.Max(_healTimer - (float)delta, 0f);
    _timeHealing = Mathf.Min(_timeHealing + (float)delta, _maxTimeHealing);
    if (Health < _maxHealth && _healTimer == 0f && _healViable)
    {
      _healTimer = Mathf.Lerp(_maxSecPerHeal, _minSecPerHeal, _timeHealing / _maxTimeHealing);
      SetHealth(Mathf.Min(Health + 1, _maxHealth));
    }

    _staminaTimer = Mathf.Max(_staminaTimer - (float)delta, 0f);
    if (Stamina < _maxStamina && _staminaTimer == 0f)
      SetStamina(Mathf.Min(Stamina + _staminaReplenishRate * (float)delta, _maxStamina));

    if (_dyingAdrenaline)
    {
      _adrenalineDeathTimer = Mathf.Max(_adrenalineDeathTimer - (float)delta, 0f);
      if (_adrenalineDeathTimer == 0f)
      {
        Die();
        return;
      }
      if (Health > 0)
        _dyingAdrenaline = false;
    }
    
    _chestClimbCast.TargetPosition = -_camera.Basis.Z with { Y = 0f };
    _hipsClimbCast.TargetPosition = -_camera.Basis.Z with { Y = 0f };

    if (CurrentState == PetraState.Running)
    {
      _chestClimbCast.TargetPosition *= 1.5f;
      _hipsClimbCast.TargetPosition *= 1.5f;
    }

    float targetColliderHeight, targetColliderY;
    if (CurrentState == PetraState.Crouching)
    {
      if (IsOnFloor())
      {
        targetColliderHeight = 2f * _initCollisionHeight / 3f;
        targetColliderY = 2f * _initCollisionY / 3f;
      }
      else
      {
        targetColliderHeight = 2f * _initCollisionHeight / 3f;
        targetColliderY = 4f * _initCollisionY / 3f;
      }
    }
    else if (CurrentState == PetraState.Sliding)
    {
      targetColliderHeight = 2f * _initCollisionHeight / 3f;
      targetColliderY = 2f * _initCollisionY / 3f;
    }
    else
    {
      targetColliderHeight = _initCollisionHeight;
      targetColliderY = _initCollisionY;
    }
    ((CapsuleShape3D)_collision.Shape).Height = Mathf.Lerp(((CapsuleShape3D)_collision.Shape).Height, targetColliderHeight, 10f * (float)delta);
    _collision.Position = _collision.Position.Lerp(_collision.Position with { Y = targetColliderY }, 10f * (float)delta);

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

    for (int i = 0; i < LastTorsoPos.Length - 1; i++)
    {
      LastTorsoPos[i] = LastTorsoPos[i + 1];
    }
    LastTorsoPos[^1] = TorsoPoint.GlobalPosition;

    for (int i = 0; i < LastHeadPos.Length - 1; i++)
    {
      LastHeadPos[i] = LastHeadPos[i + 1];
    }
    LastHeadPos[^1] = _camera.GlobalPosition;
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

    if (CurrentState is PetraState.Running)
    {
      SetStamina(Mathf.Max(Stamina - _staminaSpendRate * (float)delta, 0f));
      _staminaTimer = _staminaHealStartTime;
    }
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
    if (Input.IsActionPressed("Run") && Velocity != Vector3.Zero && Stamina > 0f)
      CurrentState = PetraState.Running;
    else if (Input.IsActionJustPressed("Crouch"))
      CurrentState = PetraState.Crouching;
  }


  private void HandleCrouchTransitions()
  {
    if (Input.IsActionPressed("Run") && Velocity != Vector3.Zero && IsOnFloor() && Stamina > 0f)
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
      if (Velocity == Vector3.Zero || !IsOnFloor())
        CurrentState = PetraState.Crouching;
    }
    if (!Input.IsActionPressed("Run") || Velocity == Vector3.Zero || Stamina <= 0f)
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
    SetHealth(Mathf.Max(Health - attack.Damage, 0));
    GD.Print("You got hit!");
    if (!_dyingAdrenaline && !_gruntPlayer.Playing)
      _gruntPlayer.Play();
    if (Health <= 0)
    {
      _healViable = false;

      if (_adrenaline)
      {
        if (!_dyingAdrenaline)
        {
          _adrenalineDeathTimer = _adrenalineDeathTime;
          _dyingAdrenaline = true;
          AdrenalineDeath?.Invoke();
        }
      }
      else
      {
        Die();
      }
    }
    else
    {
      _healTimer = _healStartTime;
      _timeHealing = -_healStartTime;
    }
  }

  private void SetHealth(int health)
  {
    Health = health;
    HealthChanged?.Invoke(Health);
  }
  private void SetStamina(float stamina)
  {
    Stamina = stamina;
    StaminaChanged?.Invoke(Stamina);
  }

  private void Die()
  {
    GD.Print("You died!");
    Died?.Invoke();

    _deathRaycast.ForceRaycastUpdate();
    _deathRaycast.ForceUpdateTransform();
    if (_deathRaycast.IsColliding())
      GlobalPosition = _deathRaycast.GetCollisionPoint();

    CurrentState = PetraState.Dead;
    _camera.Die();
  }
}
