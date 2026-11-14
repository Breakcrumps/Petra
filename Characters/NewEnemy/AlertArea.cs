using System.Collections.Generic;
using Godot;

namespace Petra.Characters.NewEnemy;

internal sealed partial class AlertArea : Area3D
{
  private readonly List<NewEnemy> _nearbyComrades = [];
  
  public override void _Ready()
  {
    BodyEntered += node =>
    {
      if (node is NewEnemy newEnemy)
        _nearbyComrades.Add(newEnemy);
    };
    BodyExited += node =>
    {
      if (node is NewEnemy newEnemy)
        _nearbyComrades.Remove(newEnemy);
    };
  }

  internal void Alert()
  {
    foreach (NewEnemy newEnemy in _nearbyComrades)
    {
      newEnemy.Alert();
    }
  }
}
