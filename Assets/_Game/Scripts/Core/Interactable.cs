using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for anything the player can use with the Interact key (E): chests, clues, runes, the portal...
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> All = new();

    [SerializeField] protected float interactRadius = 1.3f;
    [Tooltip("Offset from the pivot to the point the player must stand near.")]
    [SerializeField] protected Vector2 interactOffset = new(0f, 0.3f);

    public Vector2 InteractPoint => (Vector2)transform.position + interactOffset;
    public float InteractRadius => interactRadius;

    /// <summary>Text shown after "[E]" when the player is in range, e.g. "Buka Peti".</summary>
    public abstract string Prompt { get; }
    public virtual bool CanInteract => true;
    public abstract void Interact(PlayerController player);

    protected virtual void OnEnable() => All.Add(this);
    protected virtual void OnDisable() => All.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry() => All.Clear();

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(InteractPoint, interactRadius);
    }
}
