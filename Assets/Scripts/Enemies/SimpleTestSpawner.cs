using UnityEngine;

public class SimpleTestSpawner : MonoBehaviour
{
    public string enemyTypeToSpawn = "ChargerGrunt"; 
    public GameObject enemyPrefab; 
    public AttackData attackToTest;
    public EnemyAttackHandler targetEnemyAttackHandler;

    void Start()
    {
        if (PoolManager.Instance != null)
        {
        }
        else
        {
            Debug.LogError("PoolManager instance not found! Machingun cannot function.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GameObject enemyInstance = PoolManager.Instance.Get(
                enemyPrefab,
                transform.position,
                transform.rotation
                );



            if (enemyInstance != null)
            {
                Debug.Log("Spawned " + enemyTypeToSpawn);
            }
            else
            {
                Debug.LogError("Failed to spawn " + enemyTypeToSpawn + ". Is it configured in the pool?");
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) 
        {
            if (targetEnemyAttackHandler != null && attackToTest != null)
            {
                Debug.Log("Test script requesting attack...");
                targetEnemyAttackHandler.StartAttackExecution(attackToTest);
            }
            else
            {
                Debug.LogError("Assign AttackData and EnemyAttackHandler in TemporaryAttackTester!");
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha3)) 
        {
            if (targetEnemyAttackHandler != null)
            {
                Debug.Log("Test script requesting interrupt...");
                targetEnemyAttackHandler.InterruptAttack();
            }
        }
    }
}