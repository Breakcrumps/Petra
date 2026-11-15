using System.Threading.Tasks;
using Godot;

namespace Petra.Utils.BulletReactors;

internal readonly struct Breaker
{
  private readonly IBreakable _intact;
  private readonly PackedScene _brokenScene;
  private readonly NavigationRegion3D _navRegion;

  internal Breaker(IBreakable intact, PackedScene brokenScene, NavigationRegion3D navRegion)
  {
    _intact = intact;
    _brokenScene = brokenScene;
    _navRegion = navRegion;
  }

  internal async Task Break(Vector3 hitPos, Vector3 hitImpulse)
  {
    Broken broken = _brokenScene.Instantiate<Broken>();
    _intact.GetTree().CurrentScene.AddChild(broken);
    broken.GlobalPosition = _intact.GlobalPosition;
    broken.GlobalRotation = _intact.GlobalRotation;

    foreach (Node child in broken.GetChildren())
    {
      if (child is not RigidBody3D shard)
        continue;

      Vector3 difVector = shard.GlobalPosition - hitPos;
      Vector3 radialImpulse = difVector.Normalized() * (float)GD.RandRange(.5, 1.0);
      Vector3 forwardImpulse = hitImpulse * (float)GD.RandRange(.5, 1.0) / (1f + difVector.Length());
      shard.ApplyImpulse(radialImpulse + forwardImpulse, -difVector);

      shard.AngularVelocity = new Vector3(
        (float)GD.RandRange(-10.0, 10.0),
        (float)GD.RandRange(-10.0, 10.0),
        (float)GD.RandRange(-10.0, 10.0)
      );
    }

    _intact.QueueFree();
    broken.AudioPlayer.Play();

    SceneTree tree = _navRegion.GetTree();
    await _navRegion.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    while (_navRegion.IsBaking())
      await _navRegion.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    _navRegion.BakeNavigationMesh();
  }
}
