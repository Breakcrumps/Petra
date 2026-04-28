using Godot;

namespace Petra.Guns;

internal sealed partial class DelayedParticles : GpuParticles3D
{
  [Export] private float _delay = .5f;
  private float _timer = -1f;
  
  internal void Emit()
    => _timer = _delay;

  public override void _PhysicsProcess(double delta)
  {
    if (_timer == -1f)
      return;

    _timer -= (float)delta;

    if (_timer <= 0f)
    {
      Restart();
      Emitting = true;
      _timer = -1f;
    }
  }
}
