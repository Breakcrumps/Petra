using Godot;

internal sealed partial class CanSpawner : Node
{
  [Export] private Node3D _parent = null!;
  [Export] private PackedScene _canScene = null!;

  public override void _Ready()
  {
    for (float x = -10f; x <= 10f; x += .5f)
    {
      for (float z = -10f; z <= 10f; z += .5f)
      {
        RigidBody3D newCan = _canScene.Instantiate<RigidBody3D>();
        _parent.CallDeferred(Node.MethodName.AddChild, newCan);
        newCan.Position = new Vector3(x, 10f, z);
      }
    }
  }
}
