using Godot;
using Petra.Guns.Scripts;
using Petra.Static;

namespace Petra.Characters.Enemy;

internal sealed partial class ShootController : Node
{
  internal enum TargetBodyPart { Torso, Head }
  internal TargetBodyPart CurTargetBodyPart;
  
  [Export] private EnemyGunNode _gunNode = null!;
  [Export] private TwoBoneIK3D _aimIK = null!;
  [Export] private Marker3D _aimTarget = null!;
  [Export] private Marker3D _retractedTarget = null!;
  [Export] private RayCast3D _aimRaycast = null!;

  private bool _active;

  private float _shootTimer;

  public override void _Ready()
    => _aimIK.Active = false;

  public override void _PhysicsProcess(double delta)
  {
    if (!_active)
      return;
    
    Vector3 targetPos = (
      CurTargetBodyPart == TargetBodyPart.Torso
      ? GlobalInstances.Petra.TorsoPoint
      : GlobalInstances.Petra.HeadPoint
    ).GlobalPosition;
    
    _aimRaycast.TargetPosition = _aimRaycast.ToLocal(targetPos).Normalized() * 2f;
    _aimRaycast.ForceUpdateTransform();
    _aimRaycast.ForceRaycastUpdate();

    if (_aimRaycast.IsColliding())
    {
      _aimTarget.GlobalPosition = _aimTarget.GlobalPosition.Lerp(_retractedTarget.GlobalPosition, 20f * (float)delta);
      return;
    }
    
    _aimTarget.GlobalPosition = _aimTarget.GlobalPosition.Lerp(targetPos, 20f * (float)delta);

    if (_shootTimer <= 0f)
    {
      _gunNode.Fire();
      _shootTimer = _gunNode.GunData.DelayTime;
    }
    else
    {
      _shootTimer -= (float)delta;
    }
  }

  internal void Activate()
  {
    _aimIK.Active = true;
    _active = true;
  }

  internal void Deactivate()
  {
    _aimIK.Active = false;
    _active = false;
  }
}
