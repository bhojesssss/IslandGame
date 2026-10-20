using UnityEngine;

/// <summary>Red potion: restores health when drunk with key 1.</summary>
public class HealthPotion : Potion
{
    public override PotionType Type => PotionType.Health;
    protected override string Label => "Potion Darah";
    protected override Color TextColor => FloatingText.HealColor;
}
