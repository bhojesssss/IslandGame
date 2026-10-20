using UnityEngine;

/// <summary>One rune pillar of a RunePuzzle. Glows while it is active.</summary>
public class RuneStone : Interactable
{
    [SerializeField] RunePuzzle puzzle;
    [Tooltip("Shown in the prompt, e.g. \"Timur\".")]
    [SerializeField] string runeName = "Rune";
    [SerializeField] SpriteRenderer[] glowRenderers;
    [SerializeField] Color litColor = new(0.4f, 1f, 1f, 1f);
    [SerializeField] Color unlitColor = new(0.4f, 1f, 1f, 0.15f);

    bool lit;

    public override string Prompt => $"Sentuh Rune {runeName}";
    public override bool CanInteract => puzzle != null && !lit && !puzzle.IsSolved && !puzzle.IsBusy;

    void Start() => SetLit(false);

    public override void Interact(PlayerController player) => puzzle.Touch(this);

    public void SetLit(bool value)
    {
        lit = value;
        foreach (var glow in glowRenderers)
            if (glow != null) glow.color = value ? litColor : unlitColor;
    }

    void Update()
    {
        if (!lit) return;
        // Gentle pulse while lit.
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 4f);
        foreach (var glow in glowRenderers)
            if (glow != null) glow.color = new Color(litColor.r, litColor.g, litColor.b, litColor.a * pulse);
    }
}
