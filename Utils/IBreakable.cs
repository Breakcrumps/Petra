using Godot;

namespace Petra.Utils;

internal interface IBreakable
{
  PackedScene BrokenScene { get; }
  Breaker Breaker { get; }
}
