/// <summary>A statue where the player spends gold on upgrades.</summary>
public class UpgradeShrine : Interactable
{
    public override string Prompt => "Kuil Peningkatan";

    public override void Interact(PlayerController player) => GameUI.Instance?.ShowUpgrades();
}
