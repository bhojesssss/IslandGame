using UnityEngine;

/// <summary>
/// Drops gold and maybe a potion when an enemy dies or a pot is broken.
/// </summary>
public class LootDropper : MonoBehaviour
{
    [SerializeField] GoldCoin goldPrefab;
    [SerializeField] Vector2Int goldRange = new(3, 8);
    [Range(0f, 1f)] [SerializeField] float goldChance = 0.8f;

    [SerializeField] Potion healthPotionPrefab;
    [SerializeField] Potion manaPotionPrefab;
    [Range(0f, 1f)] [SerializeField] float potionChance = 0.25f;

    public void Drop()
    {
        Vector3 position = transform.position + Vector3.up * 0.2f;

        if (goldPrefab != null && Random.value < goldChance)
        {
            var gold = Instantiate(goldPrefab, position, Quaternion.identity);
            gold.SetAmount(Random.Range(goldRange.x, goldRange.y + 1));
            gold.Scatter();
        }

        if (Random.value < potionChance)
        {
            var prefab = Random.value < 0.5f ? healthPotionPrefab : manaPotionPrefab;
            if (prefab != null) Instantiate(prefab, position, Quaternion.identity).Scatter();
        }
    }
}
