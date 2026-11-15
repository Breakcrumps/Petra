using Godot;

namespace Petra.Utils.BulletReactors;

[GlobalClass]
internal sealed partial class Broken : Node3D
{
  [Export] internal AudioStreamPlayer3D AudioPlayer = null!;
}
