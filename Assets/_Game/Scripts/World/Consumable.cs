using UnityEngine;

/// <summary>
/// Base class for items on the ground that are used up when the player walks over them: gold coins and potions.
/// Handles what every consumable shares: bobbing, popping out of loot drops, and being pulled toward the player.
/// Subclasses decide whether the player has room for it and what collecting it gives.
/// </summary>
public abstract class Consumable : MonoBehaviour
{
    [SerializeField] protected int amount = 1;
    [SerializeField] Transform visual;
    [SerializeField] float collectRadius = 0.5f;
    [SerializeField] float magnetRadius = 1.6f;
    [SerializeField] float magnetSpeed = 6f;
    [Tooltip("Items dropped by enemies can't be collected for this long, so they visibly pop out first.")]
    [SerializeField] float spawnDelay = 0.35f;

    float bobOffset;
    float readyTime;
    Vector2 scatterVelocity;
    bool fullWarningShown;

    public int Amount => amount;
    public void SetAmount(int value) => amount = value;

    /// <summary>Makes the item pop out in a random direction (used for loot drops).</summary>
    public void Scatter()
    {
        scatterVelocity = Random.insideUnitCircle.normalized * Random.Range(1.5f, 3f);
        readyTime = Time.time + spawnDelay;
    }

    /// <summary>False when the player has no room for it; the item then stays on the ground.</summary>
    protected virtual bool CanCollect(PlayerController player) => true;

    /// <summary>Toast shown once when the player walks over it without room. Null shows nothing.</summary>
    protected virtual string FullMessage => null;

    /// <summary>Gives the item to the player. The item is destroyed right after.</summary>
    protected abstract void OnCollected(PlayerController player);

    void Start() => bobOffset = Random.value * 10f;

    void Update()
    {
        if (visual != null)
            visual.localPosition = new Vector3(0f, 0.08f * Mathf.Sin((Time.time + bobOffset) * 4f), 0f);

        if (scatterVelocity.sqrMagnitude > 0.01f)
        {
            transform.position += (Vector3)(scatterVelocity * Time.deltaTime);
            scatterVelocity = Vector2.Lerp(scatterVelocity, Vector2.zero, 8f * Time.deltaTime);
        }

        var player = PlayerController.Instance;
        if (player == null || player.IsDead || Time.time < readyTime) return;

        float distance = Vector2.Distance(transform.position, player.Center);
        if (distance <= collectRadius) TryCollect(player);
        else if (distance <= magnetRadius && CanCollect(player))
            transform.position = Vector2.MoveTowards(transform.position, player.Center, magnetSpeed * Time.deltaTime);
    }

    void TryCollect(PlayerController player)
    {
        if (!CanCollect(player))
        {
            if (!fullWarningShown && FullMessage != null) GameUI.Instance?.ShowToast(FullMessage);
            fullWarningShown = true;
            return;
        }

        OnCollected(player);
        Destroy(gameObject);
    }
}
