using Godot;
using Petra.Characters.Petra.Components;
using Petra.Objects.Guns.Scripts;

namespace Petra.Guns.Scripts;

[GlobalClass]
internal sealed partial class EnemyGunNode : GunNode
{
  [Export] private BulletSpawner _bulletSpawner = null!;
  [Export] private Marker3D _handMarker = null!;
  [Export] private Marker3D _defaultMarker = null!;
  [Export] private Marker3D _holdPos = null!;

  internal bool InHand;

  private float _shootPeriod = 1f;
  private float _shootTimer;

  public override void _Ready()
    => _bulletSpawner.BulletSpeed = GunData.BulletSpeed;

  internal void Fire()
    => _bulletSpawner.Fire();

  public override void _PhysicsProcess(double delta)
  {
    if (InHand)
    {
      GlobalRotation = _handMarker.GlobalRotation;
      GlobalPosition = _handMarker.GlobalPosition - (GlobalBasis * _holdPos.Position);

      if ((_shootTimer += (float)delta) >= _shootPeriod)
      {
        _shootTimer = 0f;
        Fire();
      }
    }
    else
    {
      GlobalPosition = _defaultMarker.GlobalPosition;
      GlobalRotation = _defaultMarker.GlobalRotation;
    }
  }
}
