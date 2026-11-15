using Godot;

namespace Petra.Utils.BulletReactors;

internal interface IBreakable
{
  PackedScene GetBrokenScene();
  Breaker GetBreaker();

  public Vector3 GlobalPosition { get; }
  public Vector3 GlobalRotation { get; }
  public SceneTree GetTree();
  public void QueueFree();
}
