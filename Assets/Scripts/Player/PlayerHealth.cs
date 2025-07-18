using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    private float blockingDamageReduction = 5;

    [Header("Runtime State")] // Visible in inspector for debugging, but generally controlled by code
    public float currentHealth;

    public event Action OnHealthChanged;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke();
        Debug.Log("Player health initialized: " + currentHealth);
    }

    public void TakeDamage(float amount, bool wasPlayerBlocking)
    {
        float actualDamageTaken = amount;

        if (wasPlayerBlocking)
        {
            // Player takes 40% of original damage while blocking (60% reduction)
            //actualDamageTaken *= 0.4f;

            // Player takes 0 damage while blocking
            actualDamageTaken = 0;

            // Player takes X amount of less damage while blocking
            //actualDamageTaken -= blockingDamageReduction;

            Debug.Log("Player blocked! Reduced damage to: " + actualDamageTaken);
        }
        currentHealth -= actualDamageTaken;

        OnHealthChanged?.Invoke();

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