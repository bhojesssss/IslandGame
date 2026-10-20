using System;
using UnityEngine;

public enum Team { Player, Enemy, Neutral }

/// <summary>
/// Hit points for anything that can be damaged: the player, enemies and breakable props.
/// Other components react through the Damaged / Died events.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] Team team = Team.Enemy;
    [SerializeField] float maxHealth = 30f;
    [Tooltip("Seconds of invulnerability after taking a hit.")]
    [SerializeField] float invulnerableTime = 0.2f;

    public Team Team => team;
    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;
    public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;

    /// <summary>Damage amount and the position the hit came from.</summary>
    public event Action<float, Vector2> Damaged;
    public event Action Died;
    public event Action Changed;

    float invulnerableUntil;

    void Awake() => Current = maxHealth;

    /// <summary>Returns true if the damage was applied.</summary>
    public bool TakeDamage(float amount, Vector2 from)
    {
        if (IsDead || amount <= 0f || Time.time < invulnerableUntil) return false;

        invulnerableUntil = Time.time + invulnerableTime;
        Current = Mathf.Max(0f, Current - amount);
        Damaged?.Invoke(amount, from);
        Changed?.Invoke();
        if (IsDead) Died?.Invoke();
        return true;
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        Changed?.Invoke();
    }

    /// <summary>Changes max HP. Any increase is also added to current HP.</summary>
    public void SetMax(float value)
    {
        float increase = Mathf.Max(0f, value - maxHealth);
        maxHealth = value;
        Current = Mathf.Clamp(Current + increase, 0f, maxHealth);
        Changed?.Invoke();
    }

    /// <summary>Can an attacker from <paramref name="attacker"/> team hurt this?</summary>
    public bool IsHostileTo(Team attacker) => attacker == Team.Player ? team != Team.Player : team == Team.Player;
}
