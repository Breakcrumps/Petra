using System;
using Godot;
using Petra.Characters;
using Petra.Guns.Scripts;
using Petra.Static;
using Petra.Types;

internal sealed partial class NewEnemy : CharacterBody3D, IDamageable
{
  private enum LookAtMode { HeadOnly, HeadAndBody, Aim }
  private LookAtMode _lookAtMode;
  
  [Export] private AnimationPlayer _animPlayer = null!;
  [Export] private LookAtModifier3D _torsoLA = null!;
  [Export] private LookAtModifier3D _neckLA = null!;
  [Export] private Marker3D _neckTarget = null!;
  [Export] private Marker3D _torsoTarget = null!;
  [Export] private Skeleton3D _skeleton = null!;
  [Export] private EnemyGunNode _gun = null!;

  public override void _Ready()
  {
    _animPlayer.Play("Idle");
    _torsoLA.Active = false;
    _torsoLA.Influence = 0f;
    _neckLA.Active = true;
    _neckLA.Influence = 0f;
  }

  public override void _PhysicsProcess(double delta)
  {
    switch (_lookAtMode)
    {
      case LookAtMode.HeadOnly:
        Attention((float)delta);
        break;
      case LookAtMode.HeadAndBody:
        FullAttention((float)delta);
        break;
      case LookAtMode.Aim:
        Aim((float)delta);
        break;
    }
  }

  private void Aim(float deltaF)
  {
    Transform3D spineBoneTransform = _skeleton.GetBoneGlobalPose(_skeleton.FindBone(_torsoLA.BoneName));
    _torsoTarget.GlobalPosition = GlobalInstances.Petra.HeadPoint.GlobalPosition;
    // _torsoTarget.GlobalPosition = GlobalInstances.Petra.HeadPoint.GlobalPosition;
    _neckTarget.GlobalPosition = GlobalInstances.Petra.HeadPoint.GlobalPosition;

    if (
      _animPlayer.CurrentAnimation != "Shoot" && _animPlayer.CurrentAnimation != "Draw"
      && _gun.CartridgesChambered != 0 && _torsoLA.Influence >= .95f
    )
    {
      _animPlayer.Play("Shoot");
    }
    if (_gun.CartridgesChambered == 0)
    {
      _animPlayer.Play("Reload");
    }

    if (_animPlayer.CurrentAnimation == "Reload" || _animPlayer.CurrentAnimation == "Draw")
    {
      _neckLA.Influence = Mathf.Lerp(_neckLA.Influence, 0f, 10f * deltaF);
      _torsoLA.Influence = Mathf.Lerp(_torsoLA.Influence, 0f, 10f * deltaF);
    }
    else
    {
      _neckLA.Influence = Mathf.Lerp(_neckLA.Influence, 1f, 10f * deltaF);
      _torsoLA.Influence = Mathf.Lerp(_torsoLA.Influence, 1f, 10f * deltaF);
    }
  }

  private void FullAttention(float deltaF)
  {
    Transform3D spineBoneTransform = _skeleton.GetBoneGlobalPose(_skeleton.FindBone(_torsoLA.BoneName));
    Vector3 bonePos = (_skeleton.GlobalTransform * spineBoneTransform).Origin;
    Vector3 difVector = GlobalInstances.Petra.GlobalPosition - GlobalPosition;
    float angle = (-GlobalBasis.Z).SignedAngleTo(difVector, Vector3.Up);
    _torsoTarget.GlobalPosition = bonePos + new Vector3(0f, 0f, -1f).Rotated(Vector3.Up, Mathf.Min(2 * Mathf.Sign(angle) * Mathf.Clamp(Math.Abs(angle) - Mathf.Pi / 2f, 0, Mathf.Pi), 3f * Mathf.Pi / 4f));
    _neckTarget.GlobalPosition = GlobalInstances.Petra.HeadPoint.GlobalPosition;
  }

  private void Attention(float deltaF)
  {
    Vector3 difVector = GlobalInstances.Petra.GlobalPosition - GlobalPosition;
    if (difVector.Dot(-GlobalBasis.Z) < 0f)
    {
      _neckLA.Influence = Mathf.Lerp(_neckLA.Influence, 0f, 5f * deltaF);
      return;
    }
    _neckLA.Influence = Mathf.Lerp(_neckLA.Influence, 1f, 5f * deltaF);
    _neckTarget.GlobalPosition = GlobalInstances.Petra.HeadPoint.GlobalPosition;
  }

  public void TakeDamage(Attack _)
  {
    if (_lookAtMode == LookAtMode.Aim)
      return;
    _animPlayer.Play("Draw");
    _gun.AnimPlayer.Play("Cock");
    _lookAtMode = LookAtMode.Aim;
    _torsoLA.Active = true;
  }
}
