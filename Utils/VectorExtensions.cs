using Godot;

internal static class VectorExtensions
{
  internal static bool IsRoughly(this Vector3 a, Vector3 b, float tolerance = 1e-5f)
    => (a - b).Length() <= tolerance;
}
