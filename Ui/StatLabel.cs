using Godot;
using Petra.Static;

namespace Petra.Ui;

internal sealed partial class StatLabel : Label
{
  private enum Stat { Health, Stamina }
  [Export] private Stat _stat;
  
  public override void _Ready()
  {
    switch (_stat)
    {
      case Stat.Health:
        UpdateText(GlobalInstances.Petra.Health);
        GlobalInstances.Petra.HealthChanged += UpdateText;
        break;
      case Stat.Stamina:
        UpdateText(GlobalInstances.Petra.Stamina);
        GlobalInstances.Petra.StaminaChanged += UpdateText;
        break;
    }
  }

  public override void _ExitTree()
  {
    switch (_stat)
    {
      case Stat.Health:
        GlobalInstances.Petra.HealthChanged -= UpdateText;
        break;
      case Stat.Stamina:
        GlobalInstances.Petra.StaminaChanged -= UpdateText;
        break;
    }
  }

  private void UpdateText(int val)
    => Text = val.ToString();

  private void UpdateText(float val)
    => Text = val.ToString("0.");
}
