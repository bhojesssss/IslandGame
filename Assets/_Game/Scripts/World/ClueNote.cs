using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A readable clue (sign, gravestone, message in a bottle...).
/// Reading it opens the clue panel, saves it in the journal and fires onFirstRead
/// (for example to reveal where a chest is buried).
/// </summary>
public class ClueNote : Interactable
{
    /// <summary>Every clue read this run, in order. Shown in the pause menu.</summary>
    public static readonly List<ClueNote> Journal = new();

    [SerializeField] string title = "Petunjuk";
    [TextArea(4, 12)]
    [SerializeField] string body = "...";
    [Tooltip("Small '!' shown above the clue until it has been read.")]
    [SerializeField] GameObject unreadMarker;
    [SerializeField] UnityEvent onFirstRead;

    bool read;

    public string Title => title;
    public string Body => body;
    public UnityEvent OnFirstRead => onFirstRead;
    public override string Prompt => "Baca";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetJournal() => Journal.Clear();

    void OnDestroy() => Journal.Remove(this);

    public override void Interact(PlayerController player)
    {
        GameUI.Instance?.ShowClue(title, body);
        if (read) return;

        read = true;
        Journal.Add(this);
        if (unreadMarker != null) unreadMarker.SetActive(false);
        onFirstRead.Invoke();
    }
}
