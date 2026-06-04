using Godot;
using Godot.Collections;
using Petra.Characters;
using Petra.Types;
using Petra.Utils;

namespace Petra.Guns;

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
    Vector3 nextPos = GlobalPosition - Basis.Z * Speed * (float)delta;
    CheckCollisions(GlobalPosition, nextPos);
    GlobalPosition = nextPos;
    
    _secsLived += (float)delta;

    if (_secsLived >= _secsToLive)
      QueueFree();
  }

  internal void CheckCollisions(Vector3 from, Vector3 to, PhysicsBody3D? excludedBody = null, bool hitBackFaces = false)
  {
    PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
    Array<Rid> exclude = [];

    if (excludedBody is not null)
      exclude.Add(excludedBody.GetRid());

    while (Damage > 0)
    {
      var query = PhysicsRayQueryParameters3D.Create(from, to);
      query.Exclude = exclude;
      query.HitBackFaces = hitBackFaces;
      query.CollisionMask = uint.MaxValue;
      Dictionary result = spaceState.IntersectRay(query);

      if (result.Count == 0)
        break;

      Vector3 hitPos = result["position"].AsVector3();
      Vector3 normal = result["normal"].AsVector3();
      Node collider = (Node)(GodotObject)result["collider"];
      Rid hitRid = result["rid"].AsRid();

      if (collider is IBreakable breakable)
        breakable.Breaker.Break(hitPos, -Speed * .0075f * Basis.Z);
      else
        SpawnDecal(hitPos, normal, collider);

      if (collider is IDamageable damageable)
        damageable.TakeDamage(new Attack(damage: Damage));

      if (collider is RigidBody3D rigidBody)
      {
        Vector3 impulse = -Speed * .0075f * Basis.Z;
        Vector3 hitPoint = hitPos - rigidBody.GlobalPosition;
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
          var exitQuery = PhysicsRayQueryParameters3D.Create(to, hitPos);
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
        from = hitPos - Basis.Z * 0.01f;
      }
      else
      {
        QueueFree();
        return;
      }
    }
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
