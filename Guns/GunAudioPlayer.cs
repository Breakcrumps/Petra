using Godot;

internal sealed partial class GunAudioPlayer : AudioStreamPlayer3D
{
  [Export] private float _lowAmmoThreshold = .4f;
  [Export] private float _maxPitch = 1.5f;
  
  internal void Play(int curAmmo, int magSize)
  {
    float ammoRatio = (float)curAmmo / magSize;
    PitchScale =
      ammoRatio <= _lowAmmoThreshold
      ? Mathf.Lerp(1f, _maxPitch, Mathf.InverseLerp(_lowAmmoThreshold, 0f, ammoRatio))
      : 1f;
    Play();
  }
}
