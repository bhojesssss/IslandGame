using UnityEngine;

/// <summary>Swaps a closed gate for an opened one. Hook Open() to a puzzle's onSolved event.</summary>
public class Gate : MonoBehaviour
{
    [SerializeField] GameObject closedVersion;
    [SerializeField] GameObject openedVersion;

    public bool IsOpen { get; private set; }

    void Awake() => Apply();

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        Apply();
    }

    void Apply()
    {
        if (closedVersion != null) closedVersion.SetActive(!IsOpen);
        if (openedVersion != null) openedVersion.SetActive(IsOpen);
    }
}
