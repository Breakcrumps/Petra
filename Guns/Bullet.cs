using System;
using Godot;
using Godot.Collections;
using Petra.Characters;
using Petra.Types;

namespace Petra.Resources.Objects.Guns;

[GlobalClass]
internal sealed partial class Bullet : Node3D
{
  [Export] private PackedScene _decalScene = null!;
  [Export] private float _secsToLive = 3f;
  private float _secsLived;

  internal int Damage;
  internal float Speed;

  public override void _PhysicsProcess(double delta)
  {
    Vector3 nextMove = -Basis.Z * Speed * (float)delta;
    Vector3 targetPos = GlobalPosition + nextMove;

    PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
    Array<Rid> exclude = [];

    while (Damage > 0)
    {
      var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, targetPos);
      query.Exclude = exclude;
      Dictionary result = spaceState.IntersectRay(query);

      if (result.Count == 0)
      {
        GlobalPosition = targetPos;
        break;
      }

      Vector3 hitPosition = result["position"].AsVector3();
      Vector3 normal = result["normal"].AsVector3();
      Node collider = (Node)(GodotObject)result["collider"];
      Rid hitRid = result["rid"].AsRid();

      SpawnDecal(hitPosition, normal, collider);

      if (collider is IDamageable damageable)
        damageable.TakeDamage(new Attack(damage: Damage));

      if (collider is RigidBody3D rigidBody)
      {
        Vector3 impulse = -Basis.Z * (Speed * .0075f);
        Vector3 hitPoint = hitPosition - rigidBody.GlobalPosition;
        float distToHitPoint = hitPoint.Length();
        float impulseMult = -7f * distToHitPoint * distToHitPoint + 1f;
        rigidBody.ApplyImpulse(impulseMult * impulse, hitPoint);
      }

      if (collider is IPenetrable penetrable)
      {
        Damage -= penetrable.DamageReduction;

        if (Damage <= 0)
        {
          QueueFree();
          return;
        }

        Array<Rid> exitExclude = [];
  
        while (true)
        {
          var exitQuery = PhysicsRayQueryParameters3D.Create(targetPos, hitPosition);
          exitQuery.Exclude = exitExclude;
          Dictionary exitResult = spaceState.IntersectRay(exitQuery);

          if (exitResult.Count == 0) 
            break;

          Node hitCollider = (Node)(GodotObject)exitResult["collider"];

          if (hitCollider == collider)
          {
            Vector3 exitPosition = exitResult["position"].AsVector3();
            Vector3 exitNormal = exitResult["normal"].AsVector3();
            SpawnDecal(exitPosition, exitNormal, collider);
            break; 
          }
          
          exitExclude.Add(exitResult["rid"].AsRid());
        }

        exclude.Add(hitRid);
        GlobalPosition = hitPosition - Basis.Z * 0.01f;
      }
      else
      {
        QueueFree();
        return;
      }
    }
    
    _secsLived += (float)delta;

    if (_secsLived >= _secsToLive)
      QueueFree();
  }

  private void SpawnDecal(Vector3 hitPosition, Vector3 normal, Node collider)
  {
    Decal bulletHole = _decalScene.Instantiate<Decal>();
    collider.AddChild(bulletHole);

    bulletHole.CullMask = collider is VisualInstance3D visual ? visual.Layers : 4;
    
    bulletHole.GlobalPosition = hitPosition;
    Vector3 upVector = Mathf.Abs(normal.Dot(Vector3.Up)) > 0.99f 
      ? Vector3.Forward 
      : Vector3.Up;
    bulletHole.LookAt(hitPosition + normal, upVector);
    bulletHole.RotateObjectLocal(Vector3.Right, -Mathf.Pi / 2f);
  }
}
