using UnityEngine;

/// <summary>Small health bar above an enemy. Only visible for a few seconds after it gets hit.</summary>
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] Health health;
    [SerializeField] SpriteRenderer background;
    [SerializeField] SpriteRenderer fill;
    [SerializeField] float width = 0.8f;
    [SerializeField] float showTime = 3f;

    float hideAt;

    void Start()
    {
        health.Changed += OnChanged;
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (health != null) health.Changed -= OnChanged;
    }

    void OnChanged()
    {
        hideAt = Time.time + showTime;
        float n = health.Normalized;
        fill.transform.localScale = new Vector3(width * n, fill.transform.localScale.y, 1f);
        fill.transform.localPosition = new Vector3(-width * 0.5f + width * n * 0.5f, 0f, 0f);
    }

    void Update() => SetVisible(Time.time < hideAt && !health.IsDead);

    void SetVisible(bool visible)
    {
        if (background.enabled == visible) return;
        background.enabled = visible;
        fill.enabled = visible;
    }
}
