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
    Vector3 nextPos = GlobalPosition - Basis.Z * Speed * (float)delta;

    PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
    var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, nextPos);
    Dictionary result = spaceState.IntersectRay(query);

    if (result.Count > 0)
    {
      Decal bulletHole = _decalScene.Instantiate<Decal>();
      GetTree().CurrentScene.AddChild(bulletHole);
      bulletHole.GlobalPosition = result["position"].AsVector3();
      Vector3 normal = result["normal"].AsVector3();
      Vector3 upVector = Mathf.Abs(normal.Dot(Vector3.Up)) > 0.99f 
        ? Vector3.Forward 
        : Vector3.Up;
      bulletHole.LookAt(bulletHole.GlobalPosition + normal, upVector);
      bulletHole.RotateObjectLocal(Vector3.Right, -Mathf.Pi / 2f);

      if ((GodotObject)result["collider"] is IDamageable damageable)
        damageable.TakeDamage(new Attack(damage: Damage));

      QueueFree();
      return;
    }
    else
    {
      GlobalPosition = nextPos;
    }
    
    _secsLived += (float)delta;

    if (_secsLived >= _secsToLive)
      QueueFree();
  }
}
