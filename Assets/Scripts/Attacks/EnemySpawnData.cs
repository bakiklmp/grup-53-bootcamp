using UnityEngine;

[System.Serializable]
public class EnemySpawnData
{
    [Tooltip("The enemy prefab to spawn (should be configured for your object pooler).")]
    public GameObject enemyPrefabToSpawn; // Or string key for pooler

    public int numberOfEnemiesToSpawn = 1;

    [Tooltip("Offset from the spawner's position or a specific spawn point transform.")]
    public Vector2 spawnPositionOffset = Vector2.zero;

    [Tooltip("If spawning multiple, this is the radius around spawnPositionOffset they can appear in.")]
    public float spawnAreaRadius = 0.5f;

    [Tooltip("Delay before the spawned enemy activates its AI (seconds).")]
    public float activationDelay = 0.1f;

    // public string onSpawnAnimationTrigger; // Animation for the spawner itself
    // public AudioClip onSpawnSound;
}