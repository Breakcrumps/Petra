
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Petra.Static;

namespace Petra.Scenes;

[GlobalClass]
internal sealed partial class CoverManager : Node
{
  internal List<CoverMarker> CoverMarkers = [];

  private Window _treeRoot = null!;

  public override void _EnterTree()
    => GlobalInstances.CoverManager = this;

  public override void _Ready()
    => _treeRoot = GetTree().Root;

  internal CoverMarker? GetBestCover(Node3D agent, Node3D target)
  {
    CoverMarker? bestMarker = null;
    float bestScore = 0f; // Non-negative so that invalid covers with negative scores doesn't win.

    foreach (CoverMarker marker in CoverMarkers)
    {
      if (marker.Occupant is not null)
        continue;

      float score = ScoreCoverMarker(marker, agent, target);

      if (score > bestScore)
      {
        bestMarker = marker;
        bestScore = score;
      }
    }

    return bestMarker;
  }

  private float ScoreCoverMarker(CoverMarker marker, Node3D agent, Node3D target)
  {
    Vector3 targetPos = target.GlobalPosition, agentPos = agent.GlobalPosition, markerPos = marker.GlobalPosition;  

    if ((-marker.GlobalBasis.Z).Dot(targetPos - markerPos) > 0)
      return -1f;

    if (!LineOfSightBlocked(markerPos + Vector3.Up, targetPos + Vector3.Up))
      return -1f;

    return (100f - agentPos.DistanceTo(markerPos)) * .5f;
  }

  private bool LineOfSightBlocked(Vector3 from, Vector3 to)
  {
    PhysicsDirectSpaceState3D spaceState = _treeRoot.GetWorld3D().DirectSpaceState;
    PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to, collisionMask: 4);
    Dictionary result = spaceState.IntersectRay(query);
    return result.Count > 0;
  }
}
