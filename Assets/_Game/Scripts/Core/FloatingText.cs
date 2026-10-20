using TMPro;
using UnityEngine;

/// <summary>
/// Small text that floats up and fades out, used for damage numbers and "+10 Emas" popups.
/// Call FloatingText.Spawn(...) from anywhere.
/// </summary>
public class FloatingText : MonoBehaviour
{
    public static readonly Color DamageColor = new(1f, 0.35f, 0.3f);
    public static readonly Color EnemyDamageColor = new(1f, 0.95f, 0.6f);
    public static readonly Color HealColor = new(0.45f, 1f, 0.45f);
    public static readonly Color ManaColor = new(0.45f, 0.7f, 1f);
    public static readonly Color GoldColor = new(1f, 0.85f, 0.25f);
    public static readonly Color CrystalColor = new(0.55f, 1f, 1f);

    const float Duration = 0.9f;
    const float RiseSpeed = 1.2f;

    TextMeshPro text;
    float age;
    Color baseColor;

    public static void Spawn(string message, Vector2 position, Color color, float size = 3.5f)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = new Vector3(position.x + Random.Range(-0.15f, 0.15f), position.y, 0f);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
        // Always on top of the world: last sorting layer, high order.
        tmp.sortingLayerID = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
        tmp.sortingOrder = 500;
        tmp.rectTransform.sizeDelta = new Vector2(6f, 1f);

        var floating = go.AddComponent<FloatingText>();
        floating.text = tmp;
        floating.baseColor = color;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

        var color = baseColor;
        color.a = 1f - Mathf.Clamp01((age - Duration * 0.5f) / (Duration * 0.5f));
        text.color = color;

        if (age >= Duration) Destroy(gameObject);
    }
}
