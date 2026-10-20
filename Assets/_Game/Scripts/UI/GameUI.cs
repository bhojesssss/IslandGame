using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Everything on screen during gameplay: HUD, interaction prompt, toast messages and all panels
/// (clue, upgrades, pause, game over, victory).
/// </summary>
public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] UnityEngine.UI.Image healthFill;
    [SerializeField] UnityEngine.UI.Image manaFill;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text manaText;
    [SerializeField] TMP_Text goldText;
    [SerializeField] TMP_Text crystalText;
    [SerializeField] TMP_Text healthPotionText;
    [SerializeField] TMP_Text manaPotionText;
    [SerializeField] TMP_Text objectiveText;

    [Header("Prompt & toast")]
    [SerializeField] GameObject promptRoot;
    [SerializeField] TMP_Text promptText;
    [SerializeField] CanvasGroup toastGroup;
    [SerializeField] TMP_Text toastText;
    [SerializeField] float toastDuration = 3f;

    [Header("Clue panel")]
    [SerializeField] GameObject cluePanel;
    [SerializeField] TMP_Text clueTitle;
    [SerializeField] TMP_Text clueBody;

    [Header("Upgrade panel")]
    [SerializeField] GameObject upgradePanel;
    [SerializeField] TMP_Text upgradeGoldText;
    [SerializeField] UpgradeRow[] upgradeRows;
    [SerializeField] GameObject upgradeFirstSelected;

    [Header("Pause / end panels")]
    [SerializeField] GameObject pausePanel;
    [SerializeField] TMP_Text journalText;
    [SerializeField] GameObject pauseFirstSelected;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text gameOverStats;
    [SerializeField] GameObject gameOverFirstSelected;
    [SerializeField] GameObject victoryPanel;
    [SerializeField] TMP_Text victoryStats;
    [SerializeField] GameObject victoryFirstSelected;

    PlayerController player;
    float toastHideTime;
    int dialogOpenedFrame;

    void Awake()
    {
        Instance = this;
        foreach (var panel in new[] { cluePanel, upgradePanel, pausePanel, gameOverPanel, victoryPanel })
            if (panel != null) panel.SetActive(false);
        if (promptRoot != null) promptRoot.SetActive(false);
        if (toastGroup != null) toastGroup.alpha = 0f;
    }

    void Start()
    {
        player = PlayerController.Instance;
        if (player == null) return;

        player.Health.Changed += RefreshHealth;
        player.Mana.Changed += RefreshMana;
        player.Inventory.Changed += RefreshInventory;
        player.Stats.Changed += RefreshUpgrades;
        RefreshHealth();
        RefreshMana();
        RefreshInventory();

        ShowToast("Kamu terdampar di pulau misterius... Cari petunjuk dan kumpulkan kristal untuk pulang!");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // Toast fade (unscaled so it also works while paused).
        if (toastGroup != null)
        {
            float target = Time.unscaledTime < toastHideTime ? 1f : 0f;
            toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, target, Time.unscaledDeltaTime * 4f);
        }

        // E closes the clue panel (not on the same frame E opened it).
        if (cluePanel.activeSelf && Time.frameCount > dialogOpenedFrame && GameInput.InteractPressed)
            CloseDialogs();
    }

    // ---------- HUD ----------

    void RefreshHealth()
    {
        healthFill.fillAmount = player.Health.Normalized;
        healthText.text = $"{Mathf.CeilToInt(player.Health.Current)} / {player.Health.Max:0}";
    }

    void RefreshMana()
    {
        manaFill.fillAmount = player.Mana.Normalized;
        manaText.text = $"{Mathf.FloorToInt(player.Mana.Current)} / {player.Mana.Max:0}";
    }

    void RefreshInventory()
    {
        var inventory = player.Inventory;
        int required = GameManager.Instance != null ? GameManager.Instance.RequiredCrystals : 3;
        goldText.text = inventory.Gold.ToString();
        crystalText.text = $"{inventory.Crystals} / {required}";
        healthPotionText.text = $"x{inventory.HealthPotions}";
        manaPotionText.text = $"x{inventory.ManaPotions}";
        objectiveText.text = inventory.Crystals >= required
            ? "Tujuan: Masuki portal kuno di utara pulau!"
            : $"Tujuan: Temukan kristal ({inventory.Crystals}/{required}) untuk menyalakan portal";
        if (upgradePanel != null && upgradePanel.activeSelf) RefreshUpgrades();
    }

    public void SetPrompt(string action)
    {
        if (promptRoot == null) return;
        bool show = !string.IsNullOrEmpty(action);
        if (promptRoot.activeSelf != show) promptRoot.SetActive(show);
        if (show) promptText.text = $"<color=#FFD966>[E]</color> {action}";
    }

    public void ShowToast(string message)
    {
        if (toastText == null) return;
        toastText.text = message;
        toastHideTime = Time.unscaledTime + toastDuration;
    }

    // ---------- Dialogs ----------

    public void ShowClue(string title, string body)
    {
        clueTitle.text = title;
        clueBody.text = body;
        cluePanel.SetActive(true);
        dialogOpenedFrame = Time.frameCount;
        GameManager.Instance?.OpenDialog();
    }

    public void ShowUpgrades()
    {
        RefreshUpgrades();
        upgradePanel.SetActive(true);
        Select(upgradeFirstSelected);
        GameManager.Instance?.OpenDialog();
    }

    public void CloseDialogs()
    {
        cluePanel.SetActive(false);
        upgradePanel.SetActive(false);
        Select(null);
        GameManager.Instance?.CloseDialog();
    }

    void RefreshUpgrades()
    {
        if (player == null) return;
        upgradeGoldText.text = $"Emas kamu: <color=#FFD966>{player.Inventory.Gold}</color>";
        foreach (var row in upgradeRows) row.Refresh(player.Stats, player.Inventory);
    }

    // ---------- Pause / end screens ----------

    public void ShowPause(bool show)
    {
        pausePanel.SetActive(show);
        if (!show) { Select(null); return; }

        var sb = new StringBuilder();
        if (ClueNote.Journal.Count == 0) sb.Append("<i>Belum ada petunjuk yang dibaca.</i>");
        foreach (var clue in ClueNote.Journal)
            sb.Append("<b><color=#FFD966>").Append(clue.Title).Append("</color></b>\n").Append(clue.Body).Append("\n\n");
        journalText.text = sb.ToString();
        Select(pauseFirstSelected);
    }

    public void ShowGameOver()
    {
        SetPrompt(null);
        gameOverStats.text = StatsSummary();
        gameOverPanel.SetActive(true);
        Select(gameOverFirstSelected);
    }

    public void ShowVictory()
    {
        SetPrompt(null);
        victoryStats.text = StatsSummary();
        victoryPanel.SetActive(true);
        Select(victoryFirstSelected);
    }

    string StatsSummary()
    {
        var gm = GameManager.Instance;
        float time = gm != null ? gm.PlayTime : 0f;
        int enemies = gm != null ? gm.EnemiesDefeated : 0;
        int crystals = player != null ? player.Inventory.Crystals : 0;
        int gold = player != null ? player.Inventory.TotalGoldCollected : 0;
        return $"Waktu bermain: {(int)time / 60:00}:{(int)time % 60:00}\n" +
               $"Kristal: {crystals}\nEmas dikumpulkan: {gold}\nMusuh dikalahkan: {enemies}";
    }

    static void Select(GameObject target)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }

    // ---------- Button callbacks (wired in the Inspector) ----------

    public void OnResumeClicked() => GameManager.Instance?.Resume();
    public void OnRestartClicked() => GameManager.Instance?.Restart();
    public void OnMainMenuClicked() => GameManager.Instance?.LoadMainMenu();
    public void OnQuitClicked() => GameManager.Instance?.Quit();
    public void OnCloseDialogClicked() => CloseDialogs();
}
