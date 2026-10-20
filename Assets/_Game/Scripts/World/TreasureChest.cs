using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A treasure chest holding gold or a crystal.
/// It can be locked (opened by a puzzle or by defeating its guards) and/or buried
/// (hidden until a clue reveals the dig spot).
/// </summary>
public class TreasureChest : Interactable
{
    public enum Content { Gold, Crystal }

    [Header("Content")]
    [SerializeField] Content content = Content.Gold;
    [SerializeField] int goldAmount = 25;

    [Header("Visuals")]
    [SerializeField] SpriteRenderer chestRenderer;
    [SerializeField] Sprite closedSprite;
    [SerializeField] Sprite openSprite;
    [SerializeField] GameObject sparkle;

    [Header("Locked")]
    [SerializeField] bool locked;
    [TextArea(2, 4)]
    [SerializeField] string lockedMessage = "Peti ini terkunci oleh sihir kuno...";

    [Header("Buried")]
    [SerializeField] bool buried;
    [Tooltip("Shown when a clue reveals where to dig.")]
    [SerializeField] GameObject digMark;

    [SerializeField] UnityEvent onOpened;

    bool opened;
    bool revealed;
    bool dug;
    Collider2D chestCollider;

    public bool IsOpened => opened;
    public Content Contents => content;
    /// <summary>Buried and not revealed by a clue yet.</summary>
    public bool IsHidden => buried && !revealed;

    public override string Prompt => buried && !dug ? "Gali di sini" : locked ? "Peti Terkunci" : "Buka Peti";
    public override bool CanInteract => !opened && (!buried || revealed);

    void Awake()
    {
        chestCollider = GetComponent<Collider2D>();
        if (buried)
        {
            chestRenderer.enabled = false;
            if (chestCollider != null) chestCollider.enabled = false;
        }
        if (digMark != null) digMark.SetActive(false);
        if (sparkle != null) sparkle.SetActive(!buried && !locked);
        if (closedSprite != null) chestRenderer.sprite = closedSprite;
    }

    /// <summary>Called by a clue: shows where the chest is buried.</summary>
    public void Reveal()
    {
        if (!buried || revealed) return;
        revealed = true;
        if (digMark != null) digMark.SetActive(true);
    }

    /// <summary>Called by a puzzle or a guard group.</summary>
    public void Unlock()
    {
        if (!locked) return;
        locked = false;
        if (sparkle != null) sparkle.SetActive(true);
        FloatingText.Spawn("Terbuka!", (Vector2)transform.position + Vector2.up, FloatingText.CrystalColor);
    }

    public override void Interact(PlayerController player)
    {
        if (buried && !dug)
        {
            Dig();
            return;
        }
        if (locked)
        {
            GameUI.Instance?.ShowToast(lockedMessage);
            return;
        }
        Open(player);
    }

    void Dig()
    {
        dug = true;
        if (digMark != null) digMark.SetActive(false);
        chestRenderer.enabled = true;
        if (chestCollider != null) chestCollider.enabled = true;
        if (sparkle != null) sparkle.SetActive(!locked);
        CameraFollow.Shake(0.08f, 0.2f);
        GameUI.Instance?.ShowToast("Kamu menggali dan menemukan sebuah peti!");
    }

    void Open(PlayerController player)
    {
        opened = true;
        if (openSprite != null) chestRenderer.sprite = openSprite;
        if (sparkle != null) sparkle.SetActive(false);

        Vector2 popupPosition = (Vector2)transform.position + Vector2.up * 1f;
        if (content == Content.Gold)
        {
            player.Inventory.AddGold(goldAmount);
            FloatingText.Spawn($"+{goldAmount} Emas", popupPosition, FloatingText.GoldColor, 4f);
            GameUI.Instance?.ShowToast($"Kamu menemukan {goldAmount} emas!");
        }
        else
        {
            player.Inventory.AddCrystal();
            int have = player.Inventory.Crystals;
            int need = GameManager.Instance != null ? GameManager.Instance.RequiredCrystals : have;
            FloatingText.Spawn("+1 Kristal", popupPosition, FloatingText.CrystalColor, 4.5f);
            GameUI.Instance?.ShowToast(have >= need
                ? "Semua kristal terkumpul! Pergilah ke portal kuno di utara."
                : $"Kristal ditemukan! ({have}/{need})");
            CameraFollow.Shake(0.1f, 0.25f);
        }
        onOpened.Invoke();
    }
}
