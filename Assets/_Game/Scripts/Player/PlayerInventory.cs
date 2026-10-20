using System;
using UnityEngine;

public enum PotionType { Health, Mana }

/// <summary>
/// Gold, crystals and potions the player carries. Potions are drunk with keys 1 (health) and 2 (mana).
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] int maxPotionsPerType = 5;
    [SerializeField] int startingHealthPotions = 1;
    [SerializeField] int startingManaPotions = 1;
    [SerializeField] float healthPotionAmount = 40f;
    [SerializeField] float manaPotionAmount = 30f;

    public int Gold { get; private set; }
    public int Crystals { get; private set; }
    public int HealthPotions { get; private set; }
    public int ManaPotions { get; private set; }
    public int MaxPotions => maxPotionsPerType;
    public int TotalGoldCollected { get; private set; }

    public event Action Changed;

    PlayerController player;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        HealthPotions = startingHealthPotions;
        ManaPotions = startingManaPotions;
    }

    void Update()
    {
        if (!player.CanAct) return;
        if (GameInput.HealthPotionPressed) UsePotion(PotionType.Health);
        if (GameInput.ManaPotionPressed) UsePotion(PotionType.Mana);
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        TotalGoldCollected += amount;
        Changed?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        Changed?.Invoke();
        return true;
    }

    public void AddCrystal()
    {
        Crystals++;
        Changed?.Invoke();
    }

    public int GetPotions(PotionType type) => type == PotionType.Health ? HealthPotions : ManaPotions;

    public bool TryAddPotion(PotionType type)
    {
        if (GetPotions(type) >= maxPotionsPerType) return false;
        if (type == PotionType.Health) HealthPotions++; else ManaPotions++;
        Changed?.Invoke();
        return true;
    }

    public void UsePotion(PotionType type)
    {
        if (GetPotions(type) <= 0)
        {
            GameUI.Instance?.ShowToast(type == PotionType.Health ? "Tidak punya potion darah." : "Tidak punya potion mana.");
            return;
        }

        if (type == PotionType.Health)
        {
            if (player.Health.Current >= player.Health.Max) { GameUI.Instance?.ShowToast("Darah sudah penuh."); return; }
            HealthPotions--;
            player.Health.Heal(healthPotionAmount);
            FloatingText.Spawn($"+{healthPotionAmount:0} HP", player.Center + Vector2.up * 0.5f, FloatingText.HealColor);
        }
        else
        {
            if (player.Mana.IsFull) { GameUI.Instance?.ShowToast("Mana sudah penuh."); return; }
            ManaPotions--;
            player.Mana.Restore(manaPotionAmount);
            FloatingText.Spawn($"+{manaPotionAmount:0} MP", player.Center + Vector2.up * 0.5f, FloatingText.ManaColor);
        }
        Changed?.Invoke();
    }
}
