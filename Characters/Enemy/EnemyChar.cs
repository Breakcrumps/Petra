using System;
using Godot;
using Petra.Types;

namespace Petra.Characters.Enemy;

internal sealed partial class EnemyChar : CharacterBody3D, IDamageable
{
  [ExportGroup("External")]
  [Export] internal PathFollow3D? CurPathFollow;

  [ExportGroup("Internal")]
  [Export] private PhysicalBoneSimulator3D _boneSim = null!;
  [Export] private CollisionShape3D _aliveCollision = null!;
  [Export] private StateMachine _stateMachine = null!;
  [Export] private AnimationPlayer _animPlayer = null!;
  [Export] private MovementController _movementController = null!;
  [Export] private ShootController _shootController = null!;

  private int _health = 100;

  internal event Action? JustHit;

  public void TakeDamage(Attack attack)
  {
    if (_health <= 0)
      return;
    
    _health -= attack.Damage;
    JustHit?.Invoke();

    if (_health <= 0f)
    {
      _aliveCollision.Disabled = true;
      _boneSim.PhysicalBonesStartSimulation();
      _stateMachine.CurState.Exit();
      _stateMachine.ProcessMode = ProcessModeEnum.Disabled;
      _animPlayer.ProcessMode = ProcessModeEnum.Disabled;
      _movementController.ProcessMode = ProcessModeEnum.Disabled;
      _shootController.ProcessMode = ProcessModeEnum.Disabled;
    }
  }
}
