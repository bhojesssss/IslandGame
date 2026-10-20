/// <summary>
/// Gold coins dropped by enemies and pots. Added to the player's gold, which is spent at the upgrade shrine.
/// </summary>
public class GoldCoin : Consumable
{
    protected override void OnCollected(PlayerController player)
    {
        player.Inventory.AddGold(amount);
        FloatingText.Spawn($"+{amount} Emas", transform.position, FloatingText.GoldColor, 3f);
    }
}
