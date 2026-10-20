using UnityEngine;

/// <summary>
/// The ancient portal that takes the player home. It wakes up once enough crystals are collected.
/// </summary>
public class Portal : Interactable
{
    [SerializeField] GameObject activeVisual;
    [SerializeField] Transform swirl;
    [SerializeField] float swirlSpeed = -90f;

    bool active;
    PlayerInventory inventory;

    int Required => GameManager.Instance != null ? GameManager.Instance.RequiredCrystals : 3;
    int Collected => inventory != null ? inventory.Crystals : 0;

    public override string Prompt => active ? "Masuk Portal" : $"Portal Kuno ({Collected}/{Required} kristal)";

    void Start()
    {
        if (activeVisual != null) activeVisual.SetActive(false);
        if (PlayerController.Instance != null)
        {
            inventory = PlayerController.Instance.Inventory;
            inventory.Changed += CheckCrystals;
        }
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.Changed -= CheckCrystals;
    }

    void Update()
    {
        if (active && swirl != null)
        {
            swirl.Rotate(0f, 0f, swirlSpeed * Time.deltaTime);
            float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 3f);
            swirl.localScale = new Vector3(pulse, pulse * 0.55f, 1f);
        }
    }

    void CheckCrystals()
    {
        if (active || Collected < Required) return;
        active = true;
        if (activeVisual != null) activeVisual.SetActive(true);
        GameUI.Instance?.ShowToast("Portal kuno telah menyala! Kembalilah ke altar di utara.");
    }

    public override void Interact(PlayerController player)
    {
        if (!active)
        {
            GameUI.Instance?.ShowToast($"Portal ini butuh {Required} kristal untuk menyala. Kamu baru punya {Collected}.");
            return;
        }
        GameManager.Instance?.Victory();
    }
}
