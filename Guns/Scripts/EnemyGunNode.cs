using Godot;
using Petra.Characters.Petra.Components;
using Petra.Objects.Guns.Scripts;
using Petra.Static;

namespace Petra.Guns.Scripts;

[GlobalClass]
internal sealed partial class EnemyGunNode : GunNode
{
  [Export] private BulletSpawner _bulletSpawner = null!;
  [Export] private Marker3D _handMarker = null!;
  [Export] private Marker3D _defaultMarker = null!;
  [Export] private Marker3D _holdPos = null!;
  [Export] private PackedScene _smokeScene = null!;
  [Export] private PackedScene _sparksScene = null!;
  [Export] private Texture2D _muzzleFlashImage = null!;
  [Export] private OmniLight3D _muzzleFlash = null!;
  [Export] private GunshotPlayer _shotSoundPlayer = null!;
  [Export] private AudioStreamPlayer3D _slideForwardAudioPlayer = null!;

  [Export] private int _shotsInARow;

  private GpuParticles3D _smoke = null!;
  private DelayedParticles _sparks = null!;
  private Sprite3D _muzzleFlashSprite = null!;

  private float _flashTimer;

  internal bool InHand;

  internal float ReactionTime;
  private Vector3 _lastLinearVel;
  private float _lastAngularVel;
  private float _lastRadialVel;

  private float _linearConfidence;
  private float _polarConfidence;


  public override void _Ready()
  {
    _bulletSpawner.BulletSpeed = GunData.BulletSpeed;
    _bulletSpawner.Damage = GunData.Damage;
    _smoke = _smokeScene.Instantiate<GpuParticles3D>();
    AddChild(_smoke);
    _sparks = _sparksScene.Instantiate<DelayedParticles>();
    AddChild(_sparks);
    _muzzleFlashSprite = new()
    {
      Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
      Transparency = 1f,
      Layers = 1,
      Texture = _muzzleFlashImage,
      Scale = new Vector3(.25f, .25f, .25f)
    };
    _bulletSpawner.AddChild(_muzzleFlashSprite);
    _muzzleFlash.LightColor = GunData.MuzzleFlashColor;
    _muzzleFlash.LightEnergy = 0f;
    CartridgesChambered = GunData.CartridgesChambered;
    CartridgesInMag = GunData.CartridgesInMag;
  }

  internal void Fire()
  {
    ShootPredict();
    _shotsInARow++;
    _smoke.GlobalPosition = _bulletSpawner.GlobalPosition;
    _smoke.Transform = _smoke.Transform.LookingAt(_smoke.GlobalPosition - _bulletSpawner.GlobalBasis.Z);
    _smoke.Restart();
    _smoke.AmountRatio = GD.Randf();
    _smoke.Emitting = true;
    _sparks.Transform = _sparks.Transform.LookingAt(_sparks.GlobalPosition - _bulletSpawner.GlobalBasis.Z);
    _sparks.Restart();
    _sparks.AmountRatio = GD.Randf();
    _sparks.Emitting = true;
    _muzzleFlashSprite.Transparency = .5f;
    _muzzleFlash.LightEnergy = 5f;
    _flashTimer = GunData.MuzzleTime;
    if (CartridgesInMag > 0)
    {
      CartridgesInMag--;
      AnimPlayer.Play("Shoot");
    }
    else
    {
      CartridgesChambered = 0;
      AnimPlayer.Play("ShootLast");
    }
    _shotSoundPlayer.Play(CartridgesInMag, GunData.CartridgesInMag);

    // void ShootPredict()
    // {
    //   int reactIdx = GlobalInstances.Petra.LastHeadPos.Length - (int)(ReactionTime * Engine.PhysicsTicksPerSecond);
    //   int olderIdx = GlobalInstances.Petra.LastHeadPos.Length - (int)(2f * ReactionTime * Engine.PhysicsTicksPerSecond);
    //   Vector3 reactPos = GlobalInstances.Petra.LastHeadPos[reactIdx];
    //   Vector3 v =
    //     (reactPos - GlobalInstances.Petra.LastHeadPos[olderIdx])
    //     / ReactionTime;
    //   if ((v - _lastVelocity).LengthSquared() <= 7f)
    //     _confidence = Mathf.Min(_confidence + .5f, 1f);
    //   else
    //     _confidence = Mathf.Max(_confidence - .6f, 0f);
    //   float t_flight = (reactPos - _bulletSpawner.GlobalPosition).Length() / _bulletSpawner.BulletSpeed;
    //   Vector3 predPoint = reactPos + v * (ReactionTime + t_flight);
    //   _bulletSpawner.Fire(reactPos.Lerp(predPoint, _confidence), _shotsInARow);
    //   _lastVelocity = v;
    // }

    void ShootPredict()
    {
      Vector3[] history = GlobalInstances.Petra.LastHeadPos;
      int len = history.Length;
      int delayTicks = Mathf.Max(1, Mathf.RoundToInt(ReactionTime * Engine.PhysicsTicksPerSecond));

      int reactIdx = Mathf.Clamp(len - 1 - delayTicks, 0, len - 1);
      // Temporary large buffer to notice wide wiggles.
      int olderIdx = Mathf.Clamp(reactIdx - 60, 0, len - 1);

      Vector3 origin = _bulletSpawner.GlobalPosition;
      Vector3 reactPos = history[reactIdx];
      Vector3 olderPos = history[olderIdx];

      float tFlight = (reactPos - origin).Length() / _bulletSpawner.BulletSpeed;
      float leadTime = ReactionTime + tFlight;

      // --- -1. Wiggle protection ---
      Vector3 point1 = olderPos;
      float pathLen = 0f, maxDistSq = 0f;

      for (int i = olderIdx + 1; i <= reactIdx; i++)
      {
        Vector3 dif = history[i] - history[i - 1];
        pathLen += (dif - dif.Project((history[i] - origin).Normalized())).Length();
        float distSq = (history[i] - olderPos).LengthSquared();
        if (distSq > maxDistSq)
        {
          maxDistSq = distSq;
          point1 = history[i];
        }
      }

      Vector3 point2 = olderPos;
      maxDistSq = (olderPos - point1).LengthSquared();

      for (int i = olderIdx + 1; i <= reactIdx; i++)
      {
        float distSq = (history[i] - point1).LengthSquared();
        if (distSq > maxDistSq)
        {
          maxDistSq = distSq;
          point2 = history[i];
        }
      }

      Vector3 move = reactPos - olderPos;
      Vector3 dirToPlayer = (reactPos - origin).Normalized();
      Vector3 lateralMove = move - move.Project(dirToPlayer);
      float netDisplacement = lateralMove.Length();
      float straightness = pathLen > .1f ? netDisplacement / pathLen : 1f;

      if (straightness < .4f)
      {
        GD.Print("Predicting wiggle");
        _linearConfidence = Mathf.Max(_linearConfidence - .2f, 0f);
        _polarConfidence  = Mathf.Max(_polarConfidence - .2f, 0f);
        Vector3 center = (point1 + point2) * .5f;
        Vector3 recentMove = reactPos - history[reactIdx - 1];
        Vector3 nonRecentMove = history[reactIdx - 1] - history[reactIdx - 2];
        float variability = recentMove.LengthSquared() - nonRecentMove.LengthSquared();
        _bulletSpawner.Fire(center.Lerp(variability >= .5f ? reactPos : history[^1], variability >= .5f ? .5f : .7f), _shotsInARow);
        return;
      }

      // Return to the tight window.
      olderIdx = Mathf.Clamp(len - 1 - (delayTicks << 1), 0, len - 1);
      olderPos = history[olderIdx];

      // --- 1. LINEAR MODEL ---
      Vector3 linearVel = (reactPos - olderPos) / ReactionTime;
      if ((linearVel - _lastLinearVel).LengthSquared() <= 4f)
        _linearConfidence = Mathf.Min(_linearConfidence + .35f, 1f);
      else
        _linearConfidence = Mathf.Max(_linearConfidence - .5f, 0f);

      _lastLinearVel = linearVel;
      Vector3 linearPred = reactPos + linearVel * leadTime;

      // --- 2. POLAR / ANGULAR MODEL ---
      Vector3 relReact = reactPos - origin;
      Vector3 relOlder = olderPos - origin;
      Vector3 flatReact = new(relReact.X, 0f, relReact.Z);
      Vector3 flatOlder = new(relOlder.X, 0f, relOlder.Z);

      float distReact = flatReact.Length();
      float distOlder = flatOlder.Length();
      float angularVel = flatOlder.SignedAngleTo(flatReact, Vector3.Up) / ReactionTime;
      float radialVel = (distReact - distOlder) / ReactionTime;
      float verticalVel = (relReact.Y - relOlder.Y) / ReactionTime;

      if (Mathf.Abs(angularVel - _lastAngularVel) <= .6f)
        _polarConfidence = Mathf.Min(_polarConfidence + .35f, 1f);
      else
        _polarConfidence = Mathf.Max(_polarConfidence - .5f, 0f);

      _lastAngularVel = angularVel;
      _lastRadialVel = radialVel;

      float predDist = Mathf.Max(.5f, distReact + radialVel * leadTime);
      Vector3 predFlatDir = flatReact.Normalized().Rotated(Vector3.Up, angularVel * leadTime);
      Vector3 polarPred = origin + (predFlatDir * predDist) + new Vector3(0f, relReact.Y + verticalVel * leadTime, 0f);

      // --- 3. BEST FIT ---      
      Vector3 bestPred;
      float bestConfidence;

      if (_polarConfidence > _linearConfidence)
      {
        GD.Print("Predicting polar");
        bestPred = polarPred;
        bestConfidence = _polarConfidence;
      }
      else
      {
        GD.Print("Predicting linear");
        bestPred = linearPred;
        bestConfidence = _linearConfidence;
      }

      _bulletSpawner.Fire(reactPos.Lerp(bestPred, bestConfidence), _shotsInARow);
    }
  }

  public override void _PhysicsProcess(double delta)
  {
    if (InHand)
    {
      GlobalRotation = _handMarker.GlobalRotation;
      GlobalPosition = _handMarker.GlobalPosition - (GlobalBasis * _holdPos.Position);
    }
    else
    {
      GlobalPosition = _defaultMarker.GlobalPosition;
      GlobalRotation = _defaultMarker.GlobalRotation;
    }

    _flashTimer = Mathf.Max(_flashTimer - (float)delta, 0f);

    if (_flashTimer == 0f)
    {
      _muzzleFlashSprite.Transparency = 1f;
      _muzzleFlash.LightEnergy = 0f;
    }
  }

  private void AudioSlideForward()
    => _slideForwardAudioPlayer.Play();
}
