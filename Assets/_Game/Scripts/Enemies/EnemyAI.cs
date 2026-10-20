using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Simple enemy brain: wander around its home, chase the player when close, attack in melee range,
/// and walk back home if the player runs too far away.
/// Guards use a small wander radius so they stay next to the treasure they protect.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    enum State { Wander, Chase, Return, Dead }

    [SerializeField] CharacterAnimator body;
    [SerializeField] Vector2 bodyCenterOffset = new(0f, 0.45f);

    [Header("Movement")]
    [SerializeField] float wanderSpeed = 1.2f;
    [SerializeField] float chaseSpeed = 2.4f;
    [SerializeField] float wanderRadius = 3f;
    [SerializeField] Vector2 wanderPause = new(1f, 3f);

    [Header("Detection")]
    [SerializeField] float detectRadius = 4.5f;
    [Tooltip("Stops chasing when the player gets this far away.")]
    [SerializeField] float loseRadius = 8f;
    [Tooltip("Gives up and goes home when this far from its home point.")]
    [SerializeField] float leashRadius = 12f;

    [Header("Attack")]
    [SerializeField] float attackRange = 0.9f;
    [SerializeField] float attackDamage = 10f;
    [SerializeField] float attackCooldown = 1.3f;
    [Tooltip("Time between starting the attack animation and the hit landing. The player can dodge in this window.")]
    [SerializeField] float attackWindup = 0.35f;

    [Header("When hit")]
    [SerializeField] float knockbackSpeed = 5f;
    [SerializeField] float stunTime = 0.25f;

    public event Action<EnemyAI> Killed;
    public bool IsDead => state == State.Dead;

    Rigidbody2D rb;
    Health health;
    State state = State.Wander;
    Vector2 home;
    Vector2 wanderTarget;
    float wanderWaitUntil;
    float stunnedUntil;
    float nextAttackTime;
    bool attacking;
    Vector2 desiredVelocity;

    Vector2 Center => (Vector2)transform.position + bodyCenterOffset;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        home = transform.position;
        wanderTarget = home;
    }

    void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    void Update()
    {
        if (state == State.Dead) return;

        desiredVelocity = Vector2.zero;
        var player = PlayerController.Instance;
        bool playerAlive = player != null && !player.IsDead;
        float toPlayer = playerAlive ? Vector2.Distance(Center, player.Center) : float.MaxValue;
        float fromHome = Vector2.Distance(transform.position, home);

        switch (state)
        {
            case State.Wander:
                if (toPlayer <= detectRadius) state = State.Chase;
                else Wander();
                break;

            case State.Chase:
                if (!playerAlive || toPlayer > loseRadius || fromHome > leashRadius) { state = State.Return; break; }
                if (toPlayer <= attackRange) TryAttack(player);
                else if (!attacking) desiredVelocity = (player.Center - Center).normalized * chaseSpeed;
                break;

            case State.Return:
                if (playerAlive && toPlayer <= detectRadius && fromHome < leashRadius * 0.5f) { state = State.Chase; break; }
                if (fromHome < 0.3f) { state = State.Wander; wanderWaitUntil = Time.time + 1f; }
                else desiredVelocity = (home - (Vector2)transform.position).normalized * chaseSpeed;
                break;
        }

        if (attacking) desiredVelocity = Vector2.zero;
        if (desiredVelocity.sqrMagnitude > 0.01f) body.Face(desiredVelocity.x);
        body.Locomotion(desiredVelocity.sqrMagnitude > 0.01f);
    }

    void FixedUpdate()
    {
        if (state == State.Dead || Time.time < stunnedUntil) return;
        rb.linearVelocity = desiredVelocity;
    }

    void Wander()
    {
        if (Time.time < wanderWaitUntil) return;

        Vector2 toTarget = wanderTarget - (Vector2)transform.position;
        if (toTarget.magnitude < 0.2f)
        {
            wanderTarget = home + UnityEngine.Random.insideUnitCircle * wanderRadius;
            wanderWaitUntil = Time.time + UnityEngine.Random.Range(wanderPause.x, wanderPause.y);
            return;
        }
        desiredVelocity = toTarget.normalized * wanderSpeed;
    }

    void TryAttack(PlayerController player)
    {
        if (attacking || Time.time < nextAttackTime) return;
        StartCoroutine(Attack(player));
    }

    IEnumerator Attack(PlayerController player)
    {
        attacking = true;
        nextAttackTime = Time.time + attackCooldown;
        body.Face(player.Center.x - Center.x);
        body.PlayAction(UnityEngine.Random.value < 0.5f ? CharacterAnimator.Attack1 : CharacterAnimator.Attack2);

        yield return new WaitForSeconds(attackWindup);

        // The hit only lands if the player is still close enough.
        if (state != State.Dead && player != null && !player.IsDead &&
            Vector2.Distance(Center, player.Center) <= attackRange + 0.35f)
        {
            player.Health.TakeDamage(attackDamage, Center);
        }

        yield return new WaitForSeconds(0.25f);
        attacking = false;
    }

    void OnDamaged(float amount, Vector2 from)
    {
        Vector2 away = ((Vector2)transform.position - from).normalized;
        rb.linearVelocity = away * knockbackSpeed;
        stunnedUntil = Time.time + stunTime;

        body.Flash(new Color(1f, 0.4f, 0.4f));
        if (!health.IsDead && !attacking) body.PlayAction(CharacterAnimator.Hurt);
        FloatingText.Spawn($"{amount:0}", Center + Vector2.up * 0.5f, FloatingText.EnemyDamageColor);

        if (state != State.Dead) state = State.Chase; // getting hit always makes it angry
    }

    void OnDied()
    {
        state = State.Dead;
        StopAllCoroutines();
        attacking = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (GameManager.Instance != null) GameManager.Instance.EnemiesDefeated++;
        Killed?.Invoke(this);
        if (TryGetComponent(out LootDropper loot)) loot.Drop();

        StartCoroutine(FadeAndDestroy(body.Die()));
    }

    IEnumerator FadeAndDestroy(float deathAnimLength)
    {
        yield return new WaitForSeconds(deathAnimLength + 0.6f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f)
        {
            body.Alpha = 1f - t;
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? (Vector3)home : transform.position;
        Gizmos.color = new Color(0.3f, 0.8f, 1f);
        Gizmos.DrawWireSphere(origin, wanderRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
