using Godot;
using Petra.Objects.Guns.Scripts;

namespace Petra.Guns.Scripts;

internal sealed partial class PetraGunNode : GunNode
{
  [Export] internal PetraGunData PetraGunData = null!;

  public override void _Ready()
    => ConfigureMeshLayers(this);

  private static void ConfigureMeshLayers(Node node)
  {
    if (node is MeshInstance3D mesh)
      mesh.Layers = 2;
    
    foreach (Node child in node.GetChildren())
    {
      ConfigureMeshLayers(child);
    }
  }
}
