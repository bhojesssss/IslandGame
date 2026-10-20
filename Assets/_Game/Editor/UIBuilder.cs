using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>Builds the uGUI canvases: in-game HUD + panels, and the main menu.</summary>
static class UIBuilder
{
    static readonly Color PanelColor = new(0.07f, 0.08f, 0.11f, 0.94f);
    static readonly Color HudColor = new(0.05f, 0.06f, 0.08f, 0.72f);
    static readonly Color Gold = new(1f, 0.85f, 0.4f);
    static readonly Color Muted = new(0.75f, 0.75f, 0.78f);
    static readonly Color ButtonColor = new(0.2f, 0.33f, 0.45f);

    static Sprite roundedSprite;

    static Sprite Rounded => roundedSprite != null
        ? roundedSprite
        : roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    // ===================================================================== Basic helpers

    static RectTransform Rect(GameObject parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Stretch(GameObject parent, string name, Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    static UnityEngine.UI.Image Image(GameObject go, Color color, Sprite sprite = null, bool raycast = false)
    {
        var image = go.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = raycast;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }
        return image;
    }

    static UnityEngine.UI.Image Icon(GameObject parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, float size)
    {
        var rt = Rect(parent, name, anchor, new Vector2(0f, 0.5f), position, new Vector2(size, size));
        var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    static TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    static UnityEngine.UI.Button Button(GameObject parent, string name, string label, Vector2 anchor, Vector2 position,
        Vector2 size, UnityAction onClick, float fontSize = 30f)
    {
        var rt = Rect(parent, name, anchor, new Vector2(0.5f, 0.5f), position, size);
        var image = Image(rt.gameObject, Color.white, Rounded, true);
        var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color(0.32f, 0.5f, 0.66f);
        colors.selectedColor = new Color(0.36f, 0.56f, 0.72f);
        colors.pressedColor = new Color(0.16f, 0.25f, 0.34f);
        colors.disabledColor = new Color(0.25f, 0.25f, 0.27f, 0.8f);
        button.colors = colors;

        var labelRt = Stretch(rt.gameObject, "Label");
        Text(labelRt, label, fontSize, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        if (onClick != null) UnityEventTools.AddPersistentListener(button.onClick, onClick);
        return button;
    }

    static GameObject Canvas(string name, int sortingOrder = 0)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return go;
    }

    public static void EventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        var module = go.AddComponent<InputSystemUIInputModule>();

        // Use the "UI" map of the project's input actions asset.
        const string assetPath = "Assets/InputSystem_Actions.inputactions";
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning("[Builder] InputSystem_Actions.inputactions not found; UI input module uses its defaults.");
            return;
        }
        // The importer stores one InputActionReference sub-asset per action; use those so the scene keeps valid links.
        var references = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<UnityEngine.InputSystem.InputActionReference>().ToArray();
        UnityEngine.InputSystem.InputActionReference Ref(string action) =>
            references.FirstOrDefault(r => r.action != null && r.action.actionMap.name == "UI" && r.action.name == action);
        module.actionsAsset = asset;
        module.point = Ref("Point");
        module.leftClick = Ref("Click");
        module.rightClick = Ref("RightClick");
        module.middleClick = Ref("MiddleClick");
        module.scrollWheel = Ref("ScrollWheel");
        module.move = Ref("Navigate");
        module.submit = Ref("Submit");
        module.cancel = Ref("Cancel");
        module.trackedDevicePosition = Ref("TrackedDevicePosition");
        module.trackedDeviceOrientation = Ref("TrackedDeviceOrientation");
    }

    /// <summary>Full-screen dim overlay with a centered window. Returns (overlay, window).</summary>
    static (GameObject overlay, GameObject window) Panel(GameObject canvas, string name, Vector2 windowSize)
    {
        var overlay = Stretch(canvas, name).gameObject;
        Image(overlay, new Color(0f, 0f, 0f, 0.78f), null, true);
        var window = Rect(overlay, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, windowSize);
        Image(window.gameObject, PanelColor, Rounded, true);
        return (overlay, window.gameObject);
    }

    static TextMeshProUGUI Title(GameObject window, string text, float y, Color color, float size = 52f)
    {
        var rt = Rect(window, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y),
            new Vector2(((RectTransform)window.transform).sizeDelta.x - 60f, 70f));
        return Text(rt, text, size, color, TextAlignmentOptions.Center, FontStyles.Bold);
    }

    // ===================================================================== In-game UI

    public static GameUI BuildGameUI(AssetsBuilder.Art art, Texture2D minimapTexture, Vector2 worldSize)
    {
        var canvas = Canvas("GameCanvas");
        var ui = canvas.AddComponent<GameUI>();

        // ---------- Status panel (top-left) ----------
        var status = Rect(canvas, "StatusPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(470f, 168f));
        Image(status.gameObject, HudColor, Rounded);

        (UnityEngine.UI.Image fill, TextMeshProUGUI text) Bar(string label, float y, Color color)
        {
            var labelRt = Rect(status.gameObject, label + "Label", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, y), new Vector2(60f, 30f));
            Text(labelRt, label, 24, color, TextAlignmentOptions.Left, FontStyles.Bold);
            var bg = Rect(status.gameObject, label + "Bar", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(76f, y), new Vector2(376f, 30f));
            Image(bg.gameObject, new Color(0f, 0f, 0f, 0.6f), Rounded);
            var fillRt = Stretch(bg.gameObject, "Fill", new Vector2(3f, 3f), new Vector2(-3f, -3f));
            var fill = Image(fillRt.gameObject, color);
            fill.sprite = art.White;
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            var textRt = Stretch(bg.gameObject, "Value");
            var text = Text(textRt, "100 / 100", 20, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            text.outlineWidth = 0.2f;
            return (fill, text);
        }
        var hp = Bar("HP", -16f, new Color(0.86f, 0.24f, 0.24f));
        var mp = Bar("MP", -54f, new Color(0.25f, 0.5f, 0.95f));

        TextMeshProUGUI Counter(string name, Sprite icon, float x, string key)
        {
            Icon(status.gameObject, name + "Icon", icon, new Vector2(0f, 1f), new Vector2(x, -122f), 40f);
            var rt = Rect(status.gameObject, name + "Text", new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(x + 44f, -122f), new Vector2(80f, 36f));
            var text = Text(rt, "0", 26, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            if (key != null)
            {
                var keyRt = Rect(status.gameObject, name + "Key", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(x + 20f, -146f), new Vector2(40f, 20f));
                Text(keyRt, key, 16, Muted, TextAlignmentOptions.Center);
            }
            return text;
        }
        var goldText = Counter("Gold", art.Coin, 18f, null);
        var crystalText = Counter("Crystal", art.Crystal, 130f, null);
        var healthPotionText = Counter("HealthPotion", art.HealthPotion, 268f, "[1]");
        var manaPotionText = Counter("ManaPotion", art.ManaPotion, 368f, "[2]");

        // ---------- Objective, toast, prompt, controls ----------
        var objective = Rect(canvas, "Objective", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(900f, 48f));
        Image(objective.gameObject, HudColor, Rounded);
        var objectiveText = Text(Stretch(objective.gameObject, "Text", new Vector2(16f, 0f), new Vector2(-16f, 0f)), "", 24, Gold);

        var toast = Rect(canvas, "Toast", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(1160f, 64f));
        Image(toast.gameObject, new Color(0.05f, 0.06f, 0.08f, 0.85f), Rounded);
        var toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toastGroup.blocksRaycasts = false;
        toastGroup.interactable = false;
        var toastText = Text(Stretch(toast.gameObject, "Text", new Vector2(20f, 4f), new Vector2(-20f, -4f)), "", 26, Color.white);

        var prompt = Rect(canvas, "Prompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(520f, 62f));
        Image(prompt.gameObject, new Color(0.05f, 0.06f, 0.08f, 0.85f), Rounded);
        var promptText = Text(Stretch(prompt.gameObject, "Text"), "[E] Buka", 30, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

        var controls = Rect(canvas, "ControlsHint", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(1500f, 34f));
        Text(controls, "WASD: Gerak    Klik Kiri / J: Pedang    Klik Kanan / K: Panah (15 MP)    E: Interaksi    1 / 2: Potion    M: Peta    Esc: Jeda",
            19, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.Left);

        BuildMinimap(canvas, art, minimapTexture, worldSize);

        // ---------- Clue panel ----------
        var (clueOverlay, clueWindow) = Panel(canvas, "CluePanel", new Vector2(980f, 620f));
        var clueTitle = Title(clueWindow, "Petunjuk", -28f, Gold);
        var clueBodyRt = Rect(clueWindow, "Body", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(880f, 420f));
        var clueBody = Text(clueBodyRt, "", 27, new Color(0.93f, 0.9f, 0.84f), TextAlignmentOptions.TopLeft);
        var clueFooter = Rect(clueWindow, "Footer", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(880f, 40f));
        Text(clueFooter, "<color=#FFD966>[E]</color> Tutup", 24, Muted);

        // ---------- Upgrade panel ----------
        var (upgradeOverlay, upgradeWindow) = Panel(canvas, "UpgradePanel", new Vector2(1000f, 720f));
        Title(upgradeWindow, "Kuil Peningkatan", -24f, Gold);
        var sub = Rect(upgradeWindow, "Subtitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(900f, 36f));
        Text(sub, "Persembahkan emas kepada patung kuno untuk menjadi lebih kuat.", 24, Muted);
        var upgradeGoldRt = Rect(upgradeWindow, "GoldText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(900f, 40f));
        var upgradeGold = Text(upgradeGoldRt, "Emas kamu: 0", 28, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

        var rows = new UpgradeRow[4];
        GameObject firstBuy = null;
        var types = new[] { UpgradeType.MaxHealth, UpgradeType.MaxMana, UpgradeType.Damage, UpgradeType.MoveSpeed };
        for (int i = 0; i < types.Length; i++)
        {
            var rowRt = Rect(upgradeWindow, $"Row_{types[i]}", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -184f - i * 112f), new Vector2(900f, 100f));
            Image(rowRt.gameObject, new Color(1f, 1f, 1f, 0.06f), Rounded);
            var nameRt = Rect(rowRt.gameObject, "Name", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -10f), new Vector2(600f, 44f));
            var nameText = Text(nameRt, "", 30, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            var detailRt = Rect(rowRt.gameObject, "Detail", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 10f), new Vector2(600f, 40f));
            var detailText = Text(detailRt, "", 24, Muted, TextAlignmentOptions.Left);

            var row = rowRt.gameObject.AddComponent<UpgradeRow>();
            var buy = Button(rowRt.gameObject, "BuyButton", "0 Emas", new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(230f, 66f), row.Buy, 26f);
            BuilderUtil.Set(row, "type", (int)types[i]);
            BuilderUtil.Set(row, "nameText", nameText);
            BuilderUtil.Set(row, "detailText", detailText);
            BuilderUtil.Set(row, "costText", buy.GetComponentInChildren<TextMeshProUGUI>());
            BuilderUtil.Set(row, "buyButton", buy);
            rows[i] = row;
            if (i == 0) firstBuy = buy.gameObject;
        }
        Button(upgradeWindow, "CloseButton", "Tutup [Esc]", new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(280f, 60f), ui.OnCloseDialogClicked, 26f);

        // ---------- Pause panel ----------
        var (pauseOverlay, pauseWindow) = Panel(canvas, "PausePanel", new Vector2(1240f, 780f));
        Title(pauseWindow, "Jeda", -24f, Color.white);
        var resume = Button(pauseWindow, "ResumeButton", "Lanjutkan", new Vector2(0f, 1f), new Vector2(210f, -170f), new Vector2(330f, 74f), ui.OnResumeClicked);
        Button(pauseWindow, "RestartButton", "Ulangi", new Vector2(0f, 1f), new Vector2(210f, -260f), new Vector2(330f, 74f), ui.OnRestartClicked);
        Button(pauseWindow, "MenuButton", "Menu Utama", new Vector2(0f, 1f), new Vector2(210f, -350f), new Vector2(330f, 74f), ui.OnMainMenuClicked);
        Button(pauseWindow, "QuitButton", "Keluar Game", new Vector2(0f, 1f), new Vector2(210f, -440f), new Vector2(330f, 74f), ui.OnQuitClicked);
        var controlsRt = Rect(pauseWindow, "Controls", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(45f, 36f), new Vector2(330f, 230f));
        Text(controlsRt, "<b>Kontrol</b>\nWASD / Panah: Gerak\nKlik Kiri / J: Pedang\nKlik Kanan / K: Panah\nE: Interaksi\n1 / 2: Potion\nM / Tab: Peta\nEsc: Jeda",
            20, Muted, TextAlignmentOptions.BottomLeft);

        var journalHeader = Rect(pauseWindow, "JournalHeader", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, -110f), new Vector2(780f, 44f));
        Text(journalHeader, "Catatan Petunjuk", 30, Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        var scroll = Rect(pauseWindow, "JournalScroll", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, -160f), new Vector2(780f, 580f));
        Image(scroll.gameObject, new Color(1f, 1f, 1f, 0.05f), Rounded, true);
        var scrollRect = scroll.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 30f;
        var viewport = Stretch(scroll.gameObject, "Viewport", new Vector2(16f, 16f), new Vector2(-16f, -16f));
        Image(viewport.gameObject, Color.white, null, true);
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        var journalRt = new GameObject("JournalText", typeof(RectTransform)).GetComponent<RectTransform>();
        journalRt.SetParent(content, false);
        var journalText = Text(journalRt, "", 22, new Color(0.93f, 0.9f, 0.84f), TextAlignmentOptions.TopLeft);
        scrollRect.viewport = viewport;
        scrollRect.content = content;

        // ---------- Game over & victory ----------
        var (gameOverOverlay, gameOverWindow) = Panel(canvas, "GameOverPanel", new Vector2(820f, 560f));
        Title(gameOverWindow, "Kamu Tewas...", -36f, new Color(1f, 0.4f, 0.35f), 60f);
        var goSub = Rect(gameOverWindow, "Subtitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(740f, 40f));
        Text(goSub, "Pulau ini terlalu berbahaya. Jangan menyerah!", 26, Muted);
        var goStatsRt = Rect(gameOverWindow, "Stats", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(600f, 180f));
        var goStats = Text(goStatsRt, "", 26, Color.white);
        var retry = Button(gameOverWindow, "RetryButton", "Coba Lagi", new Vector2(0.5f, 0f), new Vector2(-170f, 70f), new Vector2(300f, 74f), ui.OnRestartClicked);
        Button(gameOverWindow, "MenuButton", "Menu Utama", new Vector2(0.5f, 0f), new Vector2(170f, 70f), new Vector2(300f, 74f), ui.OnMainMenuClicked);

        var (victoryOverlay, victoryWindow) = Panel(canvas, "VictoryPanel", new Vector2(920f, 640f));
        Title(victoryWindow, "Kamu Berhasil Pulang!", -36f, Gold, 60f);
        var vSub = Rect(victoryWindow, "Story", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(820f, 90f));
        Text(vSub, "Ketiga kristal bersinar terang. Portal kuno terbuka dan cahayanya membawamu kembali ke rumah...", 26, Muted);
        var vStatsRt = Rect(victoryWindow, "Stats", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(600f, 180f));
        var vStats = Text(vStatsRt, "", 26, Color.white);
        var again = Button(victoryWindow, "PlayAgainButton", "Main Lagi", new Vector2(0.5f, 0f), new Vector2(-170f, 70f), new Vector2(300f, 74f), ui.OnRestartClicked);
        Button(victoryWindow, "MenuButton", "Menu Utama", new Vector2(0.5f, 0f), new Vector2(170f, 70f), new Vector2(300f, 74f), ui.OnMainMenuClicked);

        // ---------- Wire references ----------
        BuilderUtil.Set(ui, "healthFill", hp.fill);
        BuilderUtil.Set(ui, "manaFill", mp.fill);
        BuilderUtil.Set(ui, "healthText", hp.text);
        BuilderUtil.Set(ui, "manaText", mp.text);
        BuilderUtil.Set(ui, "goldText", goldText);
        BuilderUtil.Set(ui, "crystalText", crystalText);
        BuilderUtil.Set(ui, "healthPotionText", healthPotionText);
        BuilderUtil.Set(ui, "manaPotionText", manaPotionText);
        BuilderUtil.Set(ui, "objectiveText", objectiveText);
        BuilderUtil.Set(ui, "promptRoot", prompt.gameObject);
        BuilderUtil.Set(ui, "promptText", promptText);
        BuilderUtil.Set(ui, "toastGroup", toastGroup);
        BuilderUtil.Set(ui, "toastText", toastText);
        BuilderUtil.Set(ui, "cluePanel", clueOverlay);
        BuilderUtil.Set(ui, "clueTitle", clueTitle);
        BuilderUtil.Set(ui, "clueBody", clueBody);
        BuilderUtil.Set(ui, "upgradePanel", upgradeOverlay);
        BuilderUtil.Set(ui, "upgradeGoldText", upgradeGold);
        BuilderUtil.Set(ui, "upgradeRows", rows);
        BuilderUtil.Set(ui, "upgradeFirstSelected", firstBuy);
        BuilderUtil.Set(ui, "pausePanel", pauseOverlay);
        BuilderUtil.Set(ui, "journalText", journalText);
        BuilderUtil.Set(ui, "pauseFirstSelected", resume.gameObject);
        BuilderUtil.Set(ui, "gameOverPanel", gameOverOverlay);
        BuilderUtil.Set(ui, "gameOverStats", goStats);
        BuilderUtil.Set(ui, "gameOverFirstSelected", retry.gameObject);
        BuilderUtil.Set(ui, "victoryPanel", victoryOverlay);
        BuilderUtil.Set(ui, "victoryStats", vStats);
        BuilderUtil.Set(ui, "victoryFirstSelected", again.gameObject);

        // Panels start hidden (GameUI also hides them in Awake).
        foreach (var panel in new[] { clueOverlay, upgradeOverlay, pauseOverlay, gameOverOverlay, victoryOverlay })
            panel.SetActive(false);
        prompt.gameObject.SetActive(false);
        return ui;
    }

    static void BuildMinimap(GameObject canvas, AssetsBuilder.Art art, Texture2D mapTexture, Vector2 worldSize)
    {
        // 4 UI pixels per map tile keeps the pixel-art map crisp.
        var mapSize = worldSize * 4f;
        const float padding = 6f;

        var backdrop = Stretch(canvas, "MapBackdrop");
        Image(backdrop.gameObject, new Color(0f, 0f, 0f, 0.78f));

        var panel = Rect(canvas, "Minimap", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f),
            mapSize + Vector2.one * padding * 2f);
        Image(panel.gameObject, HudColor, Rounded);

        var mapArea = Stretch(panel.gameObject, "Map", Vector2.one * padding, -Vector2.one * padding);
        var map = mapArea.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        map.texture = mapTexture;
        map.raycastTarget = false;

        var fog = Stretch(mapArea.gameObject, "Fog").gameObject.AddComponent<UnityEngine.UI.RawImage>();
        fog.raycastTarget = false;
        var icons = Stretch(mapArea.gameObject, "Icons");

        var marker = Rect(mapArea.gameObject, "PlayerMarker", Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f));
        var markerImage = marker.gameObject.AddComponent<UnityEngine.UI.Image>();
        markerImage.sprite = art.MapDot;
        markerImage.raycastTarget = false;

        var label = Rect(panel.gameObject, "Label", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-4f, -2f), new Vector2(160f, 24f));
        Text(label, "[M] Peta", 17, new Color(1f, 1f, 1f, 0.75f), TextAlignmentOptions.Right);

        var minimap = panel.gameObject.AddComponent<Minimap>();
        BuilderUtil.Set(minimap, "panel", panel);
        BuilderUtil.Set(minimap, "mapArea", mapArea);
        BuilderUtil.Set(minimap, "fogImage", fog);
        BuilderUtil.Set(minimap, "playerMarker", marker);
        BuilderUtil.Set(minimap, "iconLayer", icons);
        BuilderUtil.Set(minimap, "bigMapBackdrop", backdrop.gameObject);
        BuilderUtil.Set(minimap, "worldMax", worldSize);
        BuilderUtil.Set(minimap, "crystalChestIcon", art.Crystal);
        BuilderUtil.Set(minimap, "goldChestIcon", art.Coin);
        BuilderUtil.Set(minimap, "portalIcon", art.Portal);
        BuilderUtil.Set(minimap, "shrineIcon", art.Sparkle);
        backdrop.gameObject.SetActive(false);
    }

    // ===================================================================== Main menu

    public static void BuildMainMenu(AssetsBuilder.Art art, Sprite heroSprite, Sprite[] monsterSprites)
    {
        var canvas = Canvas("MenuCanvas");
        var menu = canvas.AddComponent<MainMenuUI>();

        var bg = Stretch(canvas, "Background");
        Image(bg.gameObject, new Color(0.06f, 0.14f, 0.18f));

        // Decorative characters (pixel sprites scaled up).
        void Character(string name, Sprite sprite, Vector2 position, float size, bool flip)
        {
            if (sprite == null) return;
            var rt = Rect(canvas, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
            var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (flip) rt.localScale = new Vector3(-1f, 1f, 1f);
        }
        Character("Hero", heroSprite, new Vector2(-560f, -60f), 900f, false);
        for (int i = 0; i < monsterSprites.Length; i++)
            Character($"Monster{i}", monsterSprites[i], new Vector2(520f + i * 120f, -40f - i * 90f), 820f, true);
        var crystal = Rect(canvas, "Crystal", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(90f, 110f));
        var crystalImage = crystal.gameObject.AddComponent<UnityEngine.UI.Image>();
        crystalImage.sprite = art.Crystal;
        crystalImage.preserveAspect = true;
        crystalImage.raycastTarget = false;

        var title = Rect(canvas, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(1400f, 150f));
        var titleText = Text(title, "PULAU KRISTAL", 120, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
        titleText.outlineWidth = 0.15f;
        titleText.outlineColor = new Color32(40, 20, 0, 255);
        var subtitle = Rect(canvas, "Subtitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1200f, 50f));
        Text(subtitle, "Terdampar. Cari harta karun. Temukan jalan pulang.", 32, new Color(0.85f, 0.92f, 0.95f));

        var play = Button(canvas, "PlayButton", "Mulai Bermain", new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(440f, 84f), menu.OnPlayClicked, 34f);
        Button(canvas, "HowToPlayButton", "Cara Bermain", new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(440f, 84f), menu.OnHowToPlayClicked, 34f);
        Button(canvas, "QuitButton", "Keluar", new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(440f, 84f), menu.OnQuitClicked, 34f);

        var (howOverlay, howWindow) = Panel(canvas, "HowToPlayPanel", new Vector2(1120f, 800f));
        Title(howWindow, "Cara Bermain", -24f, Gold);
        var bodyRt = Rect(howWindow, "Body", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1020f, 560f));
        Text(bodyRt,
            "<b><color=#FFD966>Tujuan</color></b>\n" +
            "Kapalmu karam dan kamu terdampar di sebuah pulau. Kumpulkan <b>3 kristal</b> dari peti harta karun " +
            "untuk menyalakan <b>portal kuno</b> di utara pulau, lalu masuki portalnya untuk pulang.\n\n" +
            "<b><color=#FFD966>Petunjuk & Puzzle</color></b>\n" +
            "Baca catatan, papan, dan prasasti (tanda <color=#FFD966>!</color>). Petunjuk akan menunjukkan lokasi harta, " +
            "urutan puzzle rune, dan tempat menggali harta terkubur.\n\n" +
            "<b><color=#FFD966>Emas & Potion</color></b>\n" +
            "Emas dipakai untuk upgrade di <b>Kuil Peningkatan</b> (patung dekat pantai). Potion darah dan mana " +
            "bisa ditemukan dari musuh, guci, dan peti kayu.\n\n" +
            "<b><color=#FFD966>Kontrol</color></b>\n" +
            "WASD: Gerak  •  Klik Kiri / J: Pedang  •  Klik Kanan / K: Panah (pakai mana)\n" +
            "E: Interaksi  •  1: Potion darah  •  2: Potion mana  •  M: Peta  •  Esc: Jeda\n\n" +
            "Hati-hati! Kalau darahmu habis, permainan berakhir.",
            25, new Color(0.93f, 0.9f, 0.84f), TextAlignmentOptions.TopLeft);
        var back = Button(howWindow, "BackButton", "Kembali", new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(300f, 70f), menu.OnCloseHowToPlayClicked);
        howOverlay.SetActive(false);

        BuilderUtil.Set(menu, "howToPlayPanel", howOverlay);
        BuilderUtil.Set(menu, "firstSelected", play.gameObject);
        BuilderUtil.Set(menu, "howToPlayFirstSelected", back.gameObject);
    }
}
