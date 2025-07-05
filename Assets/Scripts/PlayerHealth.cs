using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;


    [Header("Runtime State")] 
    [SerializeField] private float currentHealth;


    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log("Player health initialized: " + currentHealth);
    }

    public void TakeDamage(float amount, bool wasPlayerBlocking)
    {
        float actualDamageTaken = amount;

        if (wasPlayerBlocking)
        {
            actualDamageTaken = 0;


            Debug.Log("Player blocked! Reduced damage to: " + actualDamageTaken);
        }
        currentHealth -= actualDamageTaken;
        Debug.LogWarning(gameObject.name + " took " + amount + " damage. Current health: " + currentHealth);


        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        Debug.LogError(gameObject.name + " has died! GAME OVER (placeholder).");
    }
}