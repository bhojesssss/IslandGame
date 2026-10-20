using UnityEngine;

/// <summary>
/// A potion on the ground. It goes into the potion bag (limited per type) and is drunk later with keys 1/2
/// (see PlayerInventory.UsePotion). Each potion type says which bag slot it fills and how it is labelled.
/// </summary>
public abstract class Potion : Consumable
{
    public abstract PotionType Type { get; }
    protected abstract string Label { get; }
    protected abstract Color TextColor { get; }

    protected override bool CanCollect(PlayerController player) =>
        player.Inventory.GetPotions(Type) < player.Inventory.MaxPotions;

    protected override string FullMessage => "Tas potion sudah penuh!";

    protected override void OnCollected(PlayerController player)
    {
        player.Inventory.TryAddPotion(Type);
        FloatingText.Spawn($"+1 {Label}", transform.position, TextColor, 3f);
    }
}
