using Godot;

namespace Petra.Resources.Objects.Guns;

[GlobalClass]
internal sealed partial class GunData : Resource
{
  [Export] internal Mesh Mesh = null!;
  
  [Export] internal int Damage = 50;
  [Export] internal float DelayTime = .075f;
  [Export] internal float LeanLeftAngle = Mathf.Pi / 10f;
  [Export] internal float LeanRightAngle = -Mathf.Pi / 10f;
  [Export] internal float LocalMuzzleFlashEnergy = 1f;
  [Export] internal float MuzzleTime = .05f;
  [Export] internal Color MuzzleFlashColor = new Color(255f, 220f, 135f, 255f) / 255f;

  [ExportGroup("Pivots")]
  [Export] internal Vector3 BulletSpawnerPos = new(0, 0.15040855f, -0.5375972f);
  [Export] internal Vector3 DefaultPos = new(0.18306011f, -0.35110945f, -0.58030695f);
  [Export] internal Vector3 RightLeanPos = new(0.200896f, -0.34000003f, -0.75f);
  [Export] internal Vector3 LeftLeanPos = new(0.33288902f, -0.34000003f, -0.75f);
  [Export] internal Vector3 AimPos = new(0f, -0.23659396f, -0.7946253f);
  [Export] internal Vector3 RightLeanAimPos = new(-0.07f, -0.23699999f, -0.795f);
  [Export] internal Vector3 LeftLeanAimPos = new(0.07f, -0.23699999f, -0.795f);
  [Export] internal Vector3 CrouchPos = new(0.13261843f, -0.40999997f, -0.5f);
  [Export] internal Vector3 CrouchRightLeanPos = new(0.048294246f, -0.40999997f, -0.5f);
  [Export] internal Vector3 CrouchLeftLeanPos = new(0.18103135f, -0.40999997f, -0.5f);
  [Export] internal Vector3 RunPos = new(-0.006445408f, -0.41241443f, -0.5652871f);
  [Export] internal Quaternion RunOrient = new(-0.035777166f, 0.6467056f, 0.042085633f, 0.76073694f);
  [Export] internal Vector3 BackRunPos = new(-0.006f, -0.412f, -0.622f);
  [Export] internal Quaternion BackRunOrient = new(-0.04152833f, 0.52894026f, 0.03642258f, 0.8468595f);
  [Export] internal Vector3 HeapAimPos = new(0f, -0.5767888f, -0.5083715f);
  [Export] internal Quaternion HeapAimOrient = new(0f, 0f, 0f, 1f);
  [Export] internal Vector3 SlidePos = new(0f, -0.40388823f, -0.37313437f);
  [Export] internal Vector3 NearWallPos = new(0.4801528f, -0.08312583f, -0.20461178f);
  [Export] internal Vector3 NearWallRot = new(2.1665297f, 0f, 0f);

  [ExportGroup("Weapon Sway")]
  [Export] internal float LeanSpeed = 10f;
  [Export] internal float SwayAmount = .5f;
  [Export] internal float SwayThreshold = .05f;
  [Export] internal float SwayLerpSpeed = 10f;
  [Export] internal float PullBackSpeed = 25f;
  [Export] internal float BobAmp = .05f;
  [Export] internal float BobRotAmp = .08f;
  [Export] internal float LeftRightAmp = .05f;
  [Export] internal float ReturnToPosSpeed = 15f;
  [Export] internal float AimSpeed = 20f;
  [Export] internal PosAndRot RecoilOffsetTarget = new()
  {
    Position = new(0f, .1f, .3f), Rotation = new(Mathf.Pi / 8f, 0f, 0f)
  };
  [Export] internal PosAndRot AimRecoilOffsetTarget = new()
  {
    Position = new(0f, .05f, .1f), Rotation = new(Mathf.Pi / 100f, 0f, 0f)
  };
}
