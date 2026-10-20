using System;
using UnityEngine;

public enum UpgradeType { MaxHealth, MaxMana, Damage, MoveSpeed }

/// <summary>
/// Player stats and their upgrade levels. Gold is spent here (through the upgrade shrine) to raise a stat.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Base stats")]
    [SerializeField] float baseMaxHealth = 100f;
    [SerializeField] float baseMaxMana = 50f;
    [SerializeField] float baseDamage = 10f;
    [SerializeField] float baseMoveSpeed = 4f;
    [SerializeField] float manaRegenPerSecond = 2f;

    [Header("Bonus per upgrade level")]
    [SerializeField] float healthPerLevel = 25f;
    [SerializeField] float manaPerLevel = 15f;
    [SerializeField] float damagePerLevel = 4f;
    [SerializeField] float speedPerLevel = 0.4f;

    [Header("Upgrade cost (gold)")]
    [SerializeField] int maxLevel = 5;
    [SerializeField] int baseCost = 20;
    [SerializeField] int costIncreasePerLevel = 15;

    readonly int[] levels = new int[Enum.GetValues(typeof(UpgradeType)).Length];

    public event Action Changed;

    public float MaxHealth => baseMaxHealth + healthPerLevel * Level(UpgradeType.MaxHealth);
    public float MaxMana => baseMaxMana + manaPerLevel * Level(UpgradeType.MaxMana);
    public float Damage => baseDamage + damagePerLevel * Level(UpgradeType.Damage);
    public float MoveSpeed => baseMoveSpeed + speedPerLevel * Level(UpgradeType.MoveSpeed);
    public float ManaRegen => manaRegenPerSecond;
    public int MaxLevel => maxLevel;

    public int Level(UpgradeType type) => levels[(int)type];
    public bool IsMaxed(UpgradeType type) => Level(type) >= maxLevel;
    public int Cost(UpgradeType type) => baseCost + costIncreasePerLevel * Level(type);

    public bool TryUpgrade(UpgradeType type, PlayerInventory inventory)
    {
        if (IsMaxed(type) || !inventory.SpendGold(Cost(type))) return false;
        levels[(int)type]++;
        Changed?.Invoke();
        return true;
    }

    public static string DisplayName(UpgradeType type) => type switch
    {
        UpgradeType.MaxHealth => "Darah Maksimal",
        UpgradeType.MaxMana => "Mana Maksimal",
        UpgradeType.Damage => "Kekuatan Serangan",
        UpgradeType.MoveSpeed => "Kecepatan Lari",
        _ => type.ToString()
    };

    public string CurrentValueText(UpgradeType type) => type switch
    {
        UpgradeType.MaxHealth => $"{MaxHealth:0} HP",
        UpgradeType.MaxMana => $"{MaxMana:0} MP",
        UpgradeType.Damage => $"{Damage:0} dmg",
        UpgradeType.MoveSpeed => $"{MoveSpeed:0.0} m/s",
        _ => ""
    };

    public string BonusText(UpgradeType type) => type switch
    {
        UpgradeType.MaxHealth => $"+{healthPerLevel:0} HP",
        UpgradeType.MaxMana => $"+{manaPerLevel:0} MP",
        UpgradeType.Damage => $"+{damagePerLevel:0} dmg",
        UpgradeType.MoveSpeed => $"+{speedPerLevel:0.0} m/s",
        _ => ""
    };
}
