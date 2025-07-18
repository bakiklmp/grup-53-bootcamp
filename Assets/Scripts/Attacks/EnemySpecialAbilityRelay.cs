using UnityEngine;

public class EnemySpecialAbilityRelay : MonoBehaviour
{

    void Awake()
    {
        // enemyBase = GetComponent<EnemyBase>();
        // enemyAnimator = GetComponentInChildren<Animator>(); // Or GetComponent<Animator>()
        // aiController = GetComponent<YourSpecificAIController>();
    }


    public void HandleCustomEvent(string eventName)
    {
        Debug.Log($"RELAY: {gameObject.name} received Custom Event: '{eventName}'");

        switch (eventName)
        {
            case "ExampleTeleport":
                ExecuteExampleTeleport();
                break;
            case "SpawnHealingTotem":
                ExecuteSpawnHealingTotem();
                break;
            case "BossPhase2Transition":
                ExecuteBossPhase2Transition();
                break;
            default:
                Debug.LogWarning($"RELAY: Unknown custom event name '{eventName}' received by {gameObject.name}. No action taken.");
                break;
        }
    }

    private void ExecuteExampleTeleport()
    {
        Vector2 randomOffset = Random.insideUnitCircle * 5f; 
        transform.position += (Vector3)randomOffset;
        Debug.Log($"RELAY ACTION: {gameObject.name} executed ExampleTeleport to {transform.position}");
    }

    private void ExecuteSpawnHealingTotem()
    {
        Debug.Log($"RELAY ACTION: {gameObject.name} executed SpawnHealingTotem.");
    }

    private void ExecuteBossPhase2Transition()
    {
        Debug.Log($"RELAY ACTION: {gameObject.name} executed BossPhase2Transition.");
    }
}