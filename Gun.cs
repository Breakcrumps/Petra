using Godot;

internal sealed partial class Gun : Node3D
{
  [Export] internal int Damage { get; private set; } = 50;

  [Export] private BulletSpawner _bulletSpawner = null!;
  [Export] private PetraCamera _camera = null!;
  [Export] private Node3D _aimPivot = null!;

  [Export] private Node3D _rightLeanPivot = null!;
  [Export] private Node3D _leftLeanPivot = null!;
  private Vector3 _defaultPos;
  
  [Export] private float _leanLeftAngle = Mathf.Pi / 10f;
  [Export] private float _leanRightAngle = -Mathf.Pi / 10f;
  [Export] private float _leanSpeed = 10f;

  internal Vector3 ExtPosOffset { private get; set; }
  internal Vector3 ExtRotOffset { get; set; }

  public override void _Ready()
  {
    _defaultPos = Position;
    _bulletSpawner.Camera = _camera;
  }

  public override void _PhysicsProcess(double delta)
  {
    if (Input.IsActionPressed("Aim"))
      Position = Position.Lerp(to: _aimPivot.Position, weight: _leanSpeed * (float)delta);
    else
      HandleLean(delta);
  }

  private void HandleLean(double delta)
  {
    Vector3 nextPos;
    Quaternion nextRot;
    
    float leanDir = 0f;

    if (Input.IsActionPressed("LeanLeft"))
      leanDir -= 1f;
    if (Input.IsActionPressed("LeanRight"))
      leanDir += 1f;

    if (leanDir != 0f)
    {
      nextPos = leanDir == 1f ? _rightLeanPivot.Position : _leftLeanPivot.Position;
      float leanAngle = leanDir == 1f ? _leanRightAngle : _leanLeftAngle;
      nextRot = Quaternion.FromEuler(new Vector3(0f, 0f, leanAngle));
    }
    else
    {
      nextPos = _defaultPos;
      nextRot = Quaternion.Identity;
    }

    nextPos += ExtPosOffset;
    nextRot *= Quaternion.FromEuler(ExtRotOffset);

    Position = Position.Lerp(to: nextPos, weight: _leanSpeed * (float)delta);
    Quaternion = Quaternion.Slerp(to: nextRot, weight: _leanSpeed * (float)delta);
  }
}
