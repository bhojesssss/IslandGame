using UnityEngine;

/// <summary>
/// A straight-flying projectile (the player's arrows). Damages the first hostile it touches
/// and breaks on walls, trees and other solid colliders.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 11f;
    [SerializeField] float lifetime = 1.2f;
    [Tooltip("Colliders on these layers are ignored (e.g. water, so arrows fly over the sea).")]
    [SerializeField] LayerMask ignoreLayers;

    Rigidbody2D rb;
    Team owner;
    float damage;
    Vector2 direction;
    bool spent;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Launch(Vector2 dir, float dmg, Team ownerTeam)
    {
        direction = dir.normalized;
        damage = dmg;
        owner = ownerTeam;
        transform.right = direction;
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (spent || other.isTrigger || (ignoreLayers.value & (1 << other.gameObject.layer)) != 0) return;

        var target = other.GetComponentInParent<Health>();
        if (target != null)
        {
            if (!target.IsHostileTo(owner)) return; // flies through friends
            target.TakeDamage(damage, (Vector2)transform.position - direction);
        }
        spent = true;
        Destroy(gameObject);
    }
}
