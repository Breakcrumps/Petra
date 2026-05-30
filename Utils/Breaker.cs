using Godot;

namespace Petra.Utils;

internal readonly struct Breaker
{
  private readonly Node3D _intact;
  private readonly PackedScene _brokenScene;

  internal Breaker(Node3D intact, PackedScene brokenScene)
  {
    _intact = intact;
    _brokenScene = brokenScene;
  }
  
  internal void Break()
  {
    Node3D broken = _brokenScene.Instantiate<Node3D>();
    _intact.GetTree().CurrentScene.AddChild(broken);
    broken.GlobalPosition = _intact.GlobalPosition;
    broken.GlobalRotation = _intact.GlobalRotation;

    foreach (Node child in broken.GetChildren())
    {
      if (child is not RigidBody3D shard)
        continue;

      Vector3 difVector = shard.GlobalPosition - _intact.GlobalPosition;
      shard.ApplyCentralImpulse(difVector * .5f * (float)GD.RandRange(1.5, 4.0));
    }

    _intact.QueueFree();
  }

  internal void Break(Vector3 hitPos, Vector3 hitImpulse)
  {
    Node3D broken = _brokenScene.Instantiate<Node3D>();
    _intact.GetTree().CurrentScene.AddChild(broken);
    broken.GlobalPosition = _intact.GlobalPosition;
    broken.GlobalRotation = _intact.GlobalRotation;

    foreach (Node child in broken.GetChildren())
    {
      if (child is not RigidBody3D shard)
        continue;

      Vector3 difVector = shard.GlobalPosition - hitPos;
      Vector3 radialImpulse = difVector.Normalized() * (float)GD.RandRange(2.0, 4.0);
      Vector3 forwardImpulse = hitImpulse * 7f * (float)GD.RandRange(0.5, 1.5) / (1f + difVector.Length());
      shard.ApplyCentralImpulse(radialImpulse + forwardImpulse);

      shard.AngularVelocity = new Vector3(
        GD.RandRange(-10, 10),
        GD.RandRange(-10, 10),
        GD.RandRange(-10, 10)
      );
    }

    _intact.QueueFree();
  }
}
