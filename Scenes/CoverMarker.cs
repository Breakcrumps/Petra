using Godot;
using Petra.Static;

namespace Petra.Scenes;

[GlobalClass]
internal sealed partial class CoverMarker : Marker3D
{
  private enum CoverType { FullBody, Crouch, BodyHeight }
  [Export] private CoverType _coverType;

  internal Node3D? Occupant;
  
  public override void _Ready()
    => GlobalInstances.CoverManager.CoverMarkers.Add(this);
}
