using UnityEngine;

/// <summary>
/// A prop (pot, crate, barrel) that breaks when the player hits it and may drop loot.
/// Needs a Health component with Team = Neutral.
/// </summary>
[RequireComponent(typeof(Health))]
public class Breakable : MonoBehaviour
{
    [SerializeField] Color debrisColor = new(0.55f, 0.4f, 0.3f);
    [SerializeField] Sprite debrisSprite;

    Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        health.Damaged += (_, _) => Shake();
        health.Died += Break;
    }

    void Shake() => transform.position += (Vector3)(Random.insideUnitCircle * 0.05f);

    void Break()
    {
        if (TryGetComponent(out LootDropper loot)) loot.Drop();
        if (debrisSprite != null) SpawnDebris();
        Destroy(gameObject);
    }

    void SpawnDebris()
    {
        for (int i = 0; i < 6; i++)
        {
            var piece = new GameObject("Debris");
            piece.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * 0.2f) + Vector3.up * 0.3f;
            var sr = piece.AddComponent<SpriteRenderer>();
            sr.sprite = debrisSprite;
            sr.color = debrisColor;
            sr.sortingLayerName = "Layer 1";
            sr.sortingOrder = 3;
            piece.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);
            piece.AddComponent<Debris>();
        }
    }

    /// <summary>Tiny flying chunk that fades out.</summary>
    class Debris : MonoBehaviour
    {
        Vector2 velocity;
        float life = 0.5f;
        SpriteRenderer sr;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            velocity = Random.insideUnitCircle.normalized * Random.Range(1.5f, 3f) + Vector2.up;
        }

        void Update()
        {
            life -= Time.deltaTime;
            velocity += Vector2.down * (6f * Time.deltaTime);
            transform.position += (Vector3)(velocity * Time.deltaTime);
            var c = sr.color;
            c.a = Mathf.Clamp01(life / 0.5f);
            sr.color = c;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
