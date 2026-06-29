using Godot;

namespace Petra.Utils.BulletReactors;

internal interface IBreakable
{
  PackedScene BrokenScene { get; }
  Breaker Breaker { get; }
}
