using Godot;

namespace Petra.Characters.Petra;

internal sealed partial class NegativeHealth : AudioStreamPlayer3D
{
  [Export] private PetraChar _petra = null!;
  private Tween? _fadeTween;

  public override void _Ready()
  {
    _petra.AdrenalineDeath += PlayPainSound;
    _petra.AdrenalineDeathAverted += ScheduleStop;
    _petra.Fallen += ScheduleStop;
  }

  public override void _ExitTree()
  {
    _petra.AdrenalineDeath -= PlayPainSound;
    _petra.AdrenalineDeathAverted -= ScheduleStop;
    _petra.Fallen -= ScheduleStop;
  }

  private void PlayPainSound()
  {
    _fadeTween?.Kill();
    VolumeDb = 0f;
    Play();
  }

  private void ScheduleStop()
  {
    if (!Playing)
      return;

    _fadeTween?.Kill();
    _fadeTween = CreateTween();
    _fadeTween.TweenProperty(this, "volume_db", -40f, .3f);
    _fadeTween.TweenCallback(Callable.From(Stop));
  }
}
