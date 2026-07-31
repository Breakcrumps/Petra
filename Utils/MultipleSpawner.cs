using Godot;

namespace Petra.Utils;

[GlobalClass]
internal sealed partial class MultipleSpawner : Node
{
  [Export] private bool _skipCentre;
  [Export] private float _height = 10f;
  [Export] private Vector2 _center;
  [Export] private float _step = .5f;
  [Export] private float _sideHalfLength = 20f;
  [Export] private Node3D _parent = null!;
  [Export] private PackedScene _spawnedScene = null!;
  [Export] private NavigationRegion3D? _navRegion;

  public override void _Ready()
  {
    for (float x = -_sideHalfLength + _center.X; x <= _sideHalfLength + _center.X; x += _step)
    {
      bool x_centre = Mathf.Abs(x) < 1e-9f;
      
      for (float z = -_sideHalfLength + _center.Y; z <= _sideHalfLength + _center.Y; z += _step)
      {
        if (x_centre && Mathf.Abs(z) < 1e-9f)
          continue;
        
        Node3D newSpawned = _spawnedScene.Instantiate<Node3D>();
        _parent.CallDeferred(Node.MethodName.AddChild, newSpawned);
        newSpawned.Position = new Vector3(x, _height, z);
      }
    }

    _navRegion?.CallDeferred("bake_navigation_mesh");
  }
}
