using UnityEngine;

/// <summary>
/// Finds the closest interactable in range, shows its prompt and uses it when E is pressed.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    PlayerController player;

    public Interactable Current { get; private set; }

    void Awake() => player = GetComponent<PlayerController>();

    void Update()
    {
        Current = player.CanAct ? FindNearest() : null;
        GameUI.Instance?.SetPrompt(Current != null ? Current.Prompt : null);

        if (Current != null && GameInput.InteractPressed)
            Current.Interact(player);
    }

    Interactable FindNearest()
    {
        Interactable best = null;
        float bestDistance = float.MaxValue;
        foreach (var item in Interactable.All)
        {
            if (!item.CanInteract) continue;
            float distance = Vector2.Distance(player.Center, item.InteractPoint);
            if (distance <= item.InteractRadius && distance < bestDistance)
            {
                best = item;
                bestDistance = distance;
            }
        }
        return best;
    }
}
