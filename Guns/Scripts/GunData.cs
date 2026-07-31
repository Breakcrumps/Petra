using Godot;

namespace Petra.Objects.Guns.Scripts;

[GlobalClass]
internal sealed partial class GunData : Resource
{
  [Export] internal int CartridgesInMag = 14;
  [Export] internal int CartridgesChambered = 1;
  [Export] internal int Damage = 50;
  [Export] internal float BulletSpeed = 300f;
  [Export] internal float DelayTime = .075f;
  [Export] internal float LocalMuzzleFlashEnergy = 1f;
  [Export] internal float MuzzleTime = .05f;
  [Export] internal Color MuzzleFlashColor = new Color(255f, 220f, 135f, 255f) / 255f;
}
