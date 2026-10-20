using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sword attack (left click / J) and the arrow skill that costs mana (right click / K).
/// With the mouse you aim at the cursor; with keyboard or gamepad you attack the way you are facing.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Sword")]
    [SerializeField] float attackCooldown = 0.4f;
    [Tooltip("Delay between starting the swing and the hit landing.")]
    [SerializeField] float attackHitDelay = 0.15f;
    [SerializeField] float attackReach = 0.65f;
    [SerializeField] float attackRadius = 0.7f;

    [Header("Arrow skill (uses mana)")]
    [SerializeField] Projectile arrowPrefab;
    [SerializeField] float skillManaCost = 15f;
    [SerializeField] float skillCooldown = 0.7f;
    [SerializeField] float skillFireDelay = 0.4f;
    [SerializeField] float skillDamageMultiplier = 1.5f;
    [SerializeField] int arrowCount = 3;
    [Tooltip("Angle between arrows in degrees.")]
    [SerializeField] float arrowSpread = 12f;

    [Header("Movement")]
    [Range(0f, 1f)]
    [SerializeField] float moveMultiplierWhileAttacking = 0.3f;

    public bool IsAttacking => Time.time < actionEndsAt;
    public float MoveMultiplierWhileAttacking => moveMultiplierWhileAttacking;
    public float SkillManaCost => skillManaCost;

    PlayerController player;
    float nextActionTime;
    float actionEndsAt;
    bool useSecondSwing;
    Vector2 lastSwingCenter;
    readonly HashSet<Health> hitThisSwing = new();

    void Awake() => player = GetComponent<PlayerController>();

    void Update()
    {
        if (!player.CanAct || Time.time < nextActionTime) return;

        if (GameInput.AttackPressed) StartCoroutine(SwordAttack(GetAim()));
        else if (GameInput.SkillPressed) TryArrowSkill();
    }

    Vector2 GetAim()
    {
        if (GameInput.AttackUsedMouse)
        {
            Vector2 toMouse = GameInput.MouseWorldPosition(Camera.main) - player.Center;
            if (toMouse.sqrMagnitude > 0.01f) return toMouse.normalized;
        }
        return player.Facing;
    }

    IEnumerator SwordAttack(Vector2 aim)
    {
        player.Body.Face(aim.x);
        float length = player.Body.PlayAction(useSecondSwing ? CharacterAnimator.Attack2 : CharacterAnimator.Attack1);
        useSecondSwing = !useSecondSwing;
        nextActionTime = Time.time + attackCooldown;
        actionEndsAt = Time.time + Mathf.Min(length, attackCooldown);

        yield return new WaitForSeconds(attackHitDelay);
        if (player.IsDead) yield break;

        lastSwingCenter = player.Center + aim * attackReach;
        hitThisSwing.Clear();
        foreach (var col in Physics2D.OverlapCircleAll(lastSwingCenter, attackRadius))
        {
            var target = col.GetComponentInParent<Health>();
            if (target == null || !target.IsHostileTo(Team.Player) || !hitThisSwing.Add(target)) continue;
            target.TakeDamage(player.Stats.Damage, player.Center);
        }
    }

    void TryArrowSkill()
    {
        if (arrowPrefab == null) return;
        if (!player.Mana.TrySpend(skillManaCost))
        {
            GameUI.Instance?.ShowToast("Mana tidak cukup!");
            nextActionTime = Time.time + 0.3f;
            return;
        }
        StartCoroutine(ArrowSkill(GetAim()));
    }

    IEnumerator ArrowSkill(Vector2 aim)
    {
        player.Body.Face(aim.x);
        player.Body.PlayAction(CharacterAnimator.Attack3);
        nextActionTime = Time.time + skillCooldown;
        actionEndsAt = Time.time + skillFireDelay + 0.15f;

        yield return new WaitForSeconds(skillFireDelay);
        if (player.IsDead) yield break;

        float damage = player.Stats.Damage * skillDamageMultiplier;
        for (int i = 0; i < arrowCount; i++)
        {
            float angle = (i - (arrowCount - 1) / 2f) * arrowSpread;
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * aim;
            var arrow = Instantiate(arrowPrefab, player.Center + aim * 0.3f, Quaternion.identity);
            arrow.Launch(dir, damage, Team.Player);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(lastSwingCenter, attackRadius);
    }
}
