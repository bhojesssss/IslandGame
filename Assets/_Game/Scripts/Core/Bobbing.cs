using UnityEngine;

/// <summary>Moves an object gently up and down, optionally pulsing its sprite (for markers and sparkles).</summary>
public class Bobbing : MonoBehaviour
{
    [SerializeField] float height = 0.1f;
    [SerializeField] float speed = 3f;
    [SerializeField] bool pulseAlpha;

    Vector3 startPosition;
    SpriteRenderer spriteRenderer;
    float offset;

    void Awake()
    {
        startPosition = transform.localPosition;
        spriteRenderer = GetComponent<SpriteRenderer>();
        offset = Random.value * 10f;
    }

    void Update()
    {
        float wave = Mathf.Sin((Time.time + offset) * speed);
        transform.localPosition = startPosition + Vector3.up * (wave * height);

        if (pulseAlpha && spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a = 0.55f + 0.45f * (wave * 0.5f + 0.5f);
            spriteRenderer.color = c;
        }
    }
}
