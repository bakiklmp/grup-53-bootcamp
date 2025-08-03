using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSpawnTable", menuName = "Roguelike/Spawn Table")]
public class SpawnTableSO : ScriptableObject
{
    [SerializeField]
    private List<GameObject> spawnables = new List<GameObject>();

    public GameObject GetRandomSpawnable()
    {
        if (spawnables == null || spawnables.Count == 0)
        {
            Debug.LogWarning("Spawn table is empty!");
            return null;
        }

        int randomIndex = Random.Range(0, spawnables.Count);
        return spawnables[randomIndex];
    }
}