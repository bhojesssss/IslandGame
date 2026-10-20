using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Fires onAllDefeated once every enemy in the list has been killed.
/// Used to unlock a chest that is protected by guards.
/// </summary>
public class GuardGroup : MonoBehaviour
{
    [SerializeField] EnemyAI[] guards;
    [TextArea(1, 3)]
    [SerializeField] string defeatedMessage = "Para penjaga telah dikalahkan!";
    [SerializeField] UnityEvent onAllDefeated;

    int remaining;

    public UnityEvent OnAllDefeated => onAllDefeated;

    void Start()
    {
        foreach (var guard in guards)
        {
            if (guard == null) continue;
            remaining++;
            guard.Killed += OnGuardKilled;
        }
    }

    void OnGuardKilled(EnemyAI guard)
    {
        guard.Killed -= OnGuardKilled;
        remaining--;
        if (remaining > 0) return;

        if (!string.IsNullOrEmpty(defeatedMessage)) GameUI.Instance?.ShowToast(defeatedMessage);
        onAllDefeated.Invoke();
    }
}
