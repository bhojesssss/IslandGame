using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays sprite animations by state name (Idle, Walk, Attack1, Attack2, Attack3, Hurt, Death).
/// The Animator Controller only holds the clips; the logic of which one to play lives in code.
/// </summary>
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class CharacterAnimator : MonoBehaviour
{
    public const string Idle = "Idle", Walk = "Walk", Attack1 = "Attack1", Attack2 = "Attack2",
        Attack3 = "Attack3", Hurt = "Hurt", Death = "Death";

    Animator animator;
    SpriteRenderer spriteRenderer;
    readonly Dictionary<string, float> clipLengths = new();
    string current;
    float busyUntil;
    bool dead;
    float flashUntil;
    Color flashColor;

    public SpriteRenderer Renderer => spriteRenderer;
    /// <summary>True while a one-shot animation (attack, hurt) is playing.</summary>
    public bool IsBusy => Time.time < busyUntil;
    public bool FacingLeft => spriteRenderer.flipX;
    /// <summary>Used to fade the character out (e.g. after death).</summary>
    public float Alpha { get; set; } = 1f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator.runtimeAnimatorController != null)
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                clipLengths[clip.name] = clip.length;
    }

    void Update()
    {
        // Short colour flash when hit.
        var color = Time.time < flashUntil ? flashColor : Color.white;
        color.a = Alpha;
        spriteRenderer.color = color;
    }

    public void Locomotion(bool moving)
    {
        if (dead || IsBusy) return;
        Play(moving ? Walk : Idle, false);
    }

    /// <summary>Plays a one-shot animation and returns its length in seconds.</summary>
    public float PlayAction(string state)
    {
        if (dead) return 0f;
        float length = GetLength(state);
        Play(state, true);
        busyUntil = Time.time + length;
        return length;
    }

    public float Die()
    {
        dead = true;
        busyUntil = float.MaxValue;
        Play(Death, true);
        return GetLength(Death);
    }

    public void Face(float x)
    {
        if (x > 0.01f) spriteRenderer.flipX = false;
        else if (x < -0.01f) spriteRenderer.flipX = true;
    }

    public void Flash(Color color, float duration = 0.12f)
    {
        flashColor = color;
        flashUntil = Time.time + duration;
    }

    public float GetLength(string state) => clipLengths.TryGetValue(state, out var length) ? length : 0.4f;

    void Play(string state, bool restart)
    {
        if (!restart && current == state) return;
        current = state;
        animator.Play(state, 0, 0f);
    }
}
