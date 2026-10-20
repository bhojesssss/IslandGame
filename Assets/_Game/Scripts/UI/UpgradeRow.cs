using TMPro;
using UnityEngine;

/// <summary>One line in the upgrade panel: stat name, level, current value and a buy button.</summary>
public class UpgradeRow : MonoBehaviour
{
    [SerializeField] UpgradeType type;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text detailText;
    [SerializeField] TMP_Text costText;
    [SerializeField] UnityEngine.UI.Button buyButton;

    public void Refresh(PlayerStats stats, PlayerInventory inventory)
    {
        nameText.text = $"{PlayerStats.DisplayName(type)}  <size=70%><color=#BBBBBB>Lv {stats.Level(type)}/{stats.MaxLevel}</color></size>";

        if (stats.IsMaxed(type))
        {
            detailText.text = $"{stats.CurrentValueText(type)}  <color=#8FE388>(MAKS)</color>";
            costText.text = "MAKS";
            buyButton.interactable = false;
            return;
        }

        int cost = stats.Cost(type);
        detailText.text = $"{stats.CurrentValueText(type)}  <color=#8FE388>{stats.BonusText(type)}</color>";
        costText.text = $"{cost} Emas";
        buyButton.interactable = inventory.Gold >= cost;
    }

    /// <summary>Wired to the buy button.</summary>
    public void Buy()
    {
        var player = PlayerController.Instance;
        if (player == null) return;

        if (player.Stats.TryUpgrade(type, player.Inventory))
            GameUI.Instance?.ShowToast($"{PlayerStats.DisplayName(type)} meningkat!");
        else
            GameUI.Instance?.ShowToast("Emas tidak cukup.");
    }
}
