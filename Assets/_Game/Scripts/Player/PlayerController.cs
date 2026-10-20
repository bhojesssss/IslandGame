using UnityEngine;

/// <summary>
/// Top-down movement for the player, plus reactions to getting hit and dying.
/// Other player components (combat, inventory, stats...) are reachable from here.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
[RequireComponent(typeof(PlayerStats), typeof(PlayerMana), typeof(PlayerInventory))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [SerializeField] CharacterAnimator body;
    [Tooltip("Offset from the feet (pivot) to the middle of the body.")]
    [SerializeField] Vector2 bodyCenterOffset = new(0f, 0.45f);
    [SerializeField] float knockbackSpeed = 7f;
    [SerializeField] float knockbackTime = 0.15f;
    [SerializeField] float gameOverDelay = 1.5f;

    public Health Health { get; private set; }
    public PlayerMana Mana { get; private set; }
    public PlayerStats Stats { get; private set; }
    public PlayerInventory Inventory { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public CharacterAnimator Body => body;

    public Vector2 Facing { get; private set; } = Vector2.down;
    public Vector2 Center => (Vector2)transform.position + bodyCenterOffset;
    public bool IsDead => Health.IsDead;
    public bool CanAct => !Health.IsDead && !GameManager.InputBlocked;

    Rigidbody2D rb;
    Vector2 moveInput;
    float knockbackUntil;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        Health = GetComponent<Health>();
        Mana = GetComponent<PlayerMana>();
        Stats = GetComponent<PlayerStats>();
        Inventory = GetComponent<PlayerInventory>();
        Combat = GetComponent<PlayerCombat>();
    }

    void Start()
    {
        Health.SetMax(Stats.MaxHealth);
        Mana.Setup(Stats.MaxMana, Stats.ManaRegen);

        Stats.Changed += ApplyStats;
        Health.Damaged += OnDamaged;
        Health.Died += OnDied;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        moveInput = CanAct ? Vector2.ClampMagnitude(GameInput.Move, 1f) : Vector2.zero;

        bool moving = moveInput.sqrMagnitude > 0.01f;
        if (moving)
        {
            Facing = moveInput.normalized;
            if (Combat == null || !Combat.IsAttacking) body.Face(moveInput.x);
        }
        body.Locomotion(moving);
    }

    void FixedUpdate()
    {
        if (Health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (Time.time < knockbackUntil) return;

        float speed = Stats.MoveSpeed;
        if (Combat != null && Combat.IsAttacking) speed *= Combat.MoveMultiplierWhileAttacking;
        rb.linearVelocity = moveInput * speed;
    }

    void ApplyStats()
    {
        Health.SetMax(Stats.MaxHealth);
        Mana.SetMax(Stats.MaxMana);
    }

    void OnDamaged(float amount, Vector2 from)
    {
        Vector2 away = (Vector2)transform.position - from;
        if (away.sqrMagnitude < 0.001f) away = -Facing;
        rb.linearVelocity = away.normalized * knockbackSpeed;
        knockbackUntil = Time.time + knockbackTime;

        body.Flash(new Color(1f, 0.35f, 0.35f));
        if (!Health.IsDead) body.PlayAction(CharacterAnimator.Hurt);
        FloatingText.Spawn($"-{amount:0}", Center + Vector2.up * 0.5f, FloatingText.DamageColor);
        CameraFollow.Shake(0.12f, 0.15f);
    }

    void OnDied()
    {
        body.Die();
        rb.linearVelocity = Vector2.zero;
        GameUI.Instance?.SetPrompt(null);
        Invoke(nameof(TriggerGameOver), gameOverDelay);
    }

    void TriggerGameOver() => GameManager.Instance?.GameOver();
}
