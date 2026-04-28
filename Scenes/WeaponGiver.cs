using Godot;
using Petra.Characters.Petra;
using Petra.Resources.Objects.Guns;

namespace Petra.Scenes;

[GlobalClass]
internal sealed partial class WeaponGiver : Area3D
{
  [Export] private GunData _gunData = null!;
  
  public override void _Ready() => BodyEntered += node =>
  {
    if (node is PetraChar petra)
      petra.Gun.LoadData(_gunData);
  };
}
