using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedPrefab
{
    public GameObject prefab;
    [Min(0f)] public float weight = 1f;
}

[CreateAssetMenu(fileName = "NewPrefabTable", menuName = "Roguelike/Variation Table/Prefab Table")]
public class PrefabTableSO : ScriptableObject
{
    [SerializeField]
    private List<WeightedPrefab> items;

    public GameObject GetRandomItem()
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
                return item.prefab;
            }
            roll -= item.weight;
        }

        return items[items.Count - 1].prefab;
    }
}