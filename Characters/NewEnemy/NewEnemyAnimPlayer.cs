using Godot;
using Petra.Guns.Scripts;

internal sealed partial class NewEnemyAnimPlayer : AnimationPlayer
{
  [Export] private EnemyGunNode _gun = null!;

  private void SetInHand(bool value) => _gun.InHand = value;
}
