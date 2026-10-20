using UnityEngine;

/// <summary>Blue potion: restores mana (used by the arrow skill) when drunk with key 2.</summary>
public class ManaPotion : Potion
{
    public override PotionType Type => PotionType.Mana;
    protected override string Label => "Potion Mana";
    protected override Color TextColor => FloatingText.ManaColor;
}
