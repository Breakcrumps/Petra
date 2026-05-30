using Godot;

namespace Petra.Scenes;

internal sealed partial class MultipleSpawner : Node
{
  [Export] private float _height = 10f;
  [Export] private Vector2 _center;
  [Export] private float _step = .5f;
  [Export] private Node3D _parent = null!;
  [Export] private PackedScene _spawnedScene = null!;

  public override void _Ready()
  {
    for (float x = -20f * _step + _center.X; x <= 20f * _step + _center.X; x += _step)
    {
      for (float z = -20f * _step + _center.Y; z <= 20f * _step + _center.Y; z += _step)
      {
        Node3D newSpawned = _spawnedScene.Instantiate<Node3D>();
        _parent.CallDeferred(Node.MethodName.AddChild, newSpawned);
        newSpawned.Position = new Vector3(x, _height, z);
      }
    }
  }
}
