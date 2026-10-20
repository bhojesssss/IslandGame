using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimap in the corner of the screen with fog of war: areas the player hasn't visited stay hidden.
/// Press M (or Tab) to show a bigger map.
/// The map picture is baked by the builder; the fog is a texture that gets cleared around the player.
/// </summary>
public class Minimap : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] RectTransform panel;
    [SerializeField] RectTransform mapArea;
    [SerializeField] UnityEngine.UI.RawImage fogImage;
    [SerializeField] RectTransform playerMarker;
    [SerializeField] RectTransform iconLayer;
    [SerializeField] GameObject bigMapBackdrop;

    [Header("World")]
    [SerializeField] Vector2 worldMin = Vector2.zero;
    [SerializeField] Vector2 worldMax = new(72f, 54f);

    [Header("Fog of war")]
    [SerializeField] int fogPixelsPerTile = 4;
    [Tooltip("How far around the player (in tiles) gets revealed.")]
    [SerializeField] float revealRadius = 7f;
    [SerializeField] Color32 fogColor = new(12, 16, 26, 255);

    [Header("Icons")]
    [SerializeField] Sprite crystalChestIcon;
    [SerializeField] Sprite goldChestIcon;
    [SerializeField] Sprite portalIcon;
    [SerializeField] Sprite shrineIcon;
    [SerializeField] float iconSize = 16f;

    [Header("Big map")]
    [SerializeField] float bigScale = 3f;

    class Marker
    {
        public Transform Target;
        public RectTransform Icon;
        public System.Func<bool> ShouldShow;
    }

    readonly List<Marker> markers = new();
    Texture2D fog;
    Color32[] fogPixels;
    int fogWidth, fogHeight;
    Vector2Int lastRevealCell = new(int.MinValue, int.MinValue);
    bool big;
    Vector2 smallSize, smallPosition, smallAnchor, smallPivot;

    void Start()
    {
        CreateFog();
        CreateMarkers();
        smallSize = panel.sizeDelta;
        smallPosition = panel.anchoredPosition;
        smallAnchor = panel.anchorMin;
        smallPivot = panel.pivot;
        if (bigMapBackdrop != null) bigMapBackdrop.SetActive(false);
    }

    void OnDestroy()
    {
        if (fog != null) Destroy(fog);
    }

    void Update()
    {
        var player = PlayerController.Instance;
        if (player == null) return;

        Vector2 position = player.transform.position;
        playerMarker.anchoredPosition = WorldToMap(position);

        var cell = Vector2Int.FloorToInt(position);
        if (cell != lastRevealCell)
        {
            lastRevealCell = cell;
            Reveal(position);
        }

        foreach (var marker in markers)
        {
            bool show = marker.Target != null && IsExplored(marker.Target.position) && marker.ShouldShow();
            if (marker.Icon.gameObject.activeSelf != show) marker.Icon.gameObject.SetActive(show);
            if (show) marker.Icon.anchoredPosition = WorldToMap(marker.Target.position);
        }

        if (GameInput.MapPressed && GameManager.Instance != null && GameManager.Instance.Current == GameManager.State.Playing)
            SetBig(!big);
    }

    // ---------- Fog ----------

    void CreateFog()
    {
        fogWidth = Mathf.RoundToInt((worldMax.x - worldMin.x) * fogPixelsPerTile);
        fogHeight = Mathf.RoundToInt((worldMax.y - worldMin.y) * fogPixelsPerTile);
        fog = new Texture2D(fogWidth, fogHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        fogPixels = new Color32[fogWidth * fogHeight];
        for (int i = 0; i < fogPixels.Length; i++) fogPixels[i] = fogColor;
        fog.SetPixels32(fogPixels);
        fog.Apply();
        fogImage.texture = fog;
    }

    void Reveal(Vector2 worldPosition)
    {
        Vector2 center = (worldPosition - worldMin) * fogPixelsPerTile;
        float radius = revealRadius * fogPixelsPerTile;
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(fogWidth - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(fogHeight - 1, Mathf.CeilToInt(center.y + radius));

        bool changed = false;
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
            if (distance >= 1f) continue;
            // Fully clear in the middle, soft edge near the border of the radius.
            byte alpha = (byte)(255 * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, distance)));
            int index = y * fogWidth + x;
            if (alpha >= fogPixels[index].a) continue;
            fogPixels[index].a = alpha;
            changed = true;
        }

        if (!changed) return;
        fog.SetPixels32(fogPixels);
        fog.Apply();
    }

    public bool IsExplored(Vector2 worldPosition)
    {
        Vector2 p = (worldPosition - worldMin) * fogPixelsPerTile;
        int x = Mathf.Clamp((int)p.x, 0, fogWidth - 1);
        int y = Mathf.Clamp((int)p.y, 0, fogHeight - 1);
        return fogPixels[y * fogWidth + x].a < 128;
    }

    // ---------- Markers ----------

    void CreateMarkers()
    {
        foreach (var chest in FindObjectsByType<TreasureChest>(FindObjectsSortMode.None))
        {
            var c = chest;
            AddMarker(c.transform, c.Contents == TreasureChest.Content.Crystal ? crystalChestIcon : goldChestIcon,
                () => !c.IsOpened && !c.IsHidden);
        }
        foreach (var portal in FindObjectsByType<Portal>(FindObjectsSortMode.None))
            AddMarker(portal.transform, portalIcon, () => true, 1.4f);
        foreach (var shrine in FindObjectsByType<UpgradeShrine>(FindObjectsSortMode.None))
            AddMarker(shrine.transform, shrineIcon, () => true);
    }

    void AddMarker(Transform target, Sprite sprite, System.Func<bool> shouldShow, float sizeMultiplier = 1f)
    {
        if (sprite == null) return;
        var go = new GameObject(target.name + " Icon", typeof(RectTransform));
        go.transform.SetParent(iconLayer, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.one * iconSize * sizeMultiplier;
        var image = go.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        go.SetActive(false);
        markers.Add(new Marker { Target = target, Icon = rt, ShouldShow = shouldShow });
    }

    Vector2 WorldToMap(Vector2 world)
    {
        Vector2 n = new((world.x - worldMin.x) / (worldMax.x - worldMin.x), (world.y - worldMin.y) / (worldMax.y - worldMin.y));
        return Vector2.Scale(n, mapArea.rect.size);
    }

    // ---------- Big map ----------

    void SetBig(bool value)
    {
        big = value;
        if (bigMapBackdrop != null) bigMapBackdrop.SetActive(big);
        if (big)
        {
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = smallSize * bigScale;
        }
        else
        {
            panel.anchorMin = panel.anchorMax = smallAnchor;
            panel.pivot = smallPivot;
            panel.anchoredPosition = smallPosition;
            panel.sizeDelta = smallSize;
        }
        float iconScale = big ? 1.6f : 1f;
        foreach (var marker in markers) marker.Icon.localScale = Vector3.one * iconScale;
        playerMarker.localScale = Vector3.one * iconScale;
    }
}
