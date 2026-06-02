using System;
using Godot;

namespace Petra.Guns;

internal sealed partial class GunNode : Node3D
{
  internal event Action? ShellEjected;
  internal event Action? AmmoRefilled;

  internal void EjectShell() => ShellEjected?.Invoke();
  internal void RefillAmmo() => AmmoRefilled?.Invoke();
}
