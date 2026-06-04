using Godot;
using Petra.Characters.Petra.Components;
using Petra.Static;
using Petra.Types;

namespace Petra.Characters.Enemy;

internal sealed partial class EnemyChar : CharacterBody3D, IDamageable
{
  [Export] private float _speed = 1f;
  [Export] private BulletSpawner _bulletSpawner = null!;

  private int _health = 100;
  private float _shootTimer;

  public override void _PhysicsProcess(double delta)
  {
    Vector3 difVector = GlobalInstances.Petra.GlobalPosition - GlobalPosition;
    
    if (difVector.Length() < 20f)
    {
      Basis newBasis = Basis.LookingAt(difVector);
      Quaternion = Quaternion.Slerp(newBasis.GetRotationQuaternion(), 10f * (float)delta);
      Velocity = difVector.Normalized() * _speed;
      MoveAndSlide();
    }

    _shootTimer += (float)delta;

    if (_shootTimer >= 1.5f)
    {
      _bulletSpawner.Fire();
      _shootTimer %= 1.5f;
    }
  }

  public void TakeDamage(Attack attack)
  {
    _health -= attack.Damage;

    if (_health <= 0f)
      QueueFree();
  }
}
