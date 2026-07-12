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
    }
  }
}
