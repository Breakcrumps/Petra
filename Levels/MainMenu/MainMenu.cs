using Godot;

namespace Petra.Levels.MainMenu;

[Tool]
internal sealed partial class MainMenu : Node3D
{
  public override void _Ready()
  {
    GetNode("Girl").GetNode<AnimationPlayer>("AnimationPlayer").Play("MenuSprawl");
  }
}
