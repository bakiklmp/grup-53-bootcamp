using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class WeightedTile
{
    public TileBase tile;
    [Min(0f)] public float weight = 1f;
}

[CreateAssetMenu(fileName = "NewTileTable", menuName = "Roguelike/Variation Table/Tile Table")]
public class TileTableSO : ScriptableObject
{
    [SerializeField]
    private List<WeightedTile> items;

    public TileBase GetRandomItem()
    {
        if (items == null || items.Count == 0) return null;

        float totalWeight = 0;
        foreach (var item in items)
        {
            totalWeight += item.weight;
        }

        float roll = Random.Range(0f, totalWeight);

        foreach (var item in items)
        {
            if (roll <= item.weight)
            {
                return item.tile;
            }
            roll -= item.weight;
        }

        return items[items.Count - 1].tile;
    }
}