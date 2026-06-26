using Godot;
using Petra.Characters.Petra.Components;
using Petra.Scenes;
using Petra.Static;
using Petra.Types;

namespace Petra.Characters.Enemy;

internal sealed partial class EnemyChar : CharacterBody3D, IDamageable
{
  [Export] private float _speed = 7f;
  [Export] private BulletSpawner _bulletSpawner = null!;
  [Export] private NavigationAgent3D _navAgent = null!;
  [Export] private PackedScene _corpseScene = null!;

  private int _health = 100;

  private Vector3 _velocity;

  public override void _PhysicsProcess(double delta)
    => ComputeVelocityAndOrient(delta);

  private void ComputeVelocityAndOrient(double delta)
  {
    _velocity = Vector3.Zero;
    
    CoverMarker? bestCover = GlobalInstances.CoverManager.GetBestCover(this, GlobalInstances.Petra);

    if (bestCover is null)
      return;

    _navAgent.TargetPosition = bestCover.GlobalPosition;

    if (!_navAgent.IsTargetReachable())
      return;

    Vector3 difVector = _navAgent.GetNextPathPosition() with { Y = 0f } - GlobalPosition;

    if (difVector.LengthSquared() < .01f)
    {
      Quaternion = Quaternion.Slerp(Basis.LookingAt(GlobalPosition - bestCover.Basis.Z).GetRotationQuaternion(), 10f * (float)delta);
      return;
    }
    
    _velocity = difVector.Normalized() * _speed * (float)delta;
    GlobalPosition += _velocity;
    Quaternion = Quaternion.Slerp(Basis.LookingAt(difVector).GetRotationQuaternion(), 10f * (float)delta);
  }

  public void TakeDamage(Attack attack)
  {
    _health -= attack.Damage;

    if (_health <= 0f)
    {
      RigidBody3D corpse = _corpseScene.Instantiate<RigidBody3D>();
      GetParent().AddChild(corpse);
      corpse.GlobalTransform = GlobalTransform;
      corpse.ApplyCentralImpulse(corpse.Mass * _velocity);
      QueueFree();
    }
  }
}
