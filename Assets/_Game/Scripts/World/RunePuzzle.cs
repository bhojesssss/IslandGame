using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sequence puzzle: touch the rune stones in the right order.
/// A wrong stone resets the puzzle; the right order fires onSolved (e.g. opens a gate).
/// </summary>
public class RunePuzzle : MonoBehaviour
{
    [Tooltip("The correct order. Element 0 must be touched first.")]
    [SerializeField] RuneStone[] correctOrder;
    [TextArea(1, 3)]
    [SerializeField] string wrongMessage = "Rune meredup... urutannya salah.";
    [TextArea(1, 3)]
    [SerializeField] string solvedMessage = "Keempat rune menyala! Terdengar suara gerbang terbuka...";
    [SerializeField] UnityEvent onSolved;
    [SerializeField] UnityEvent onFailed;

    readonly List<RuneStone> touched = new();
    bool resetting;

    public bool IsSolved { get; private set; }
    public bool IsBusy => resetting;
    public UnityEvent OnSolved => onSolved;
    public UnityEvent OnFailed => onFailed;

    public void Touch(RuneStone stone)
    {
        if (IsSolved || resetting || touched.Contains(stone)) return;

        touched.Add(stone);
        stone.SetLit(true);

        if (correctOrder[touched.Count - 1] != stone)
        {
            StartCoroutine(ResetAfterDelay());
            return;
        }

        if (touched.Count == correctOrder.Length)
        {
            IsSolved = true;
            GameUI.Instance?.ShowToast(solvedMessage);
            CameraFollow.Shake(0.12f, 0.4f);
            onSolved.Invoke();
        }
    }

    IEnumerator ResetAfterDelay()
    {
        resetting = true;
        GameUI.Instance?.ShowToast(wrongMessage);
        yield return new WaitForSeconds(0.8f);
        foreach (var stone in touched) stone.SetLit(false);
        touched.Clear();
        resetting = false;
        onFailed.Invoke();
    }
}
