using Godot;
using Petra.Scenes;
using Petra.Static;
using Petra.Types;

namespace Petra.Characters.Enemy;

internal sealed partial class EnemyChar : CharacterBody3D, IDamageable
{
  [Export] private float _speed = 7f;
  [Export] private NavigationAgent3D _navAgent = null!;
  [Export] private PhysicalBoneSimulator3D _boneSim = null!;
  [Export] private CollisionShape3D _aliveCollision = null!;
  [Export] private Skeleton3D _skeleton = null!;
  [Export] private AnimationPlayer _animPlayer = null!;

  private int _health = 100;
  private Vector3 _velocity;

  public override void _PhysicsProcess(double delta)
  {
    if (_health <= 0)
      return;
    
    ComputeVelocityAndOrient(delta);

    if (_velocity == Vector3.Zero)
      _animPlayer.Play("Idle");
    else
      _animPlayer.Play("Run");
  }

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
    if (_health <= 0)
      return;
    
    _health -= attack.Damage;

    if (_health <= 0f)
    {
      _aliveCollision.Disabled = true;
      _animPlayer.Stop();
      _boneSim.PhysicalBonesStartSimulation();
    }
  }
}
