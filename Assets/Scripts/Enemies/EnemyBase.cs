using UnityEngine;
using System.Collections;

public class EnemyBase : MonoBehaviour, IPoolable
{
    [Header("Configuration")]
    public EnemyData enemyData; 

    [Header("Runtime State")]
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isCurrentlyStaggered = false;

    public SpriteRenderer spriteRenderer;
    private PoolableObjectInfo _poolInfo;

    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 1f;
    public Color originalSpriteColor;


    void Awake()
    {
        if (enemyData == null)
        {
            Debug.LogError("EnemyData not assigned to " + gameObject.name + "!");
            return;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("Spriterenderer not assigned to " + gameObject.name + "!");
            return;
        }
        originalSpriteColor = spriteRenderer.color;
        _poolInfo = GetComponent<PoolableObjectInfo>();
        if (_poolInfo == null)
        {
            Debug.LogWarning($"Enemy {name} is missing PoolableObjectInfo. It might not be properly pooled.", this);
        }

    }
    public void OnObjectSpawn()
    {

        InitializeEnemy();

        Debug.Log($"{gameObject.name} spawned from pool. Position: {transform.position}");

    }
    public void OnObjectDespawn()
    {
        Debug.Log($"{gameObject.name} despawned to pool.");

    }
    void InitializeEnemy()
    {
        currentHealth = enemyData.maxHealth;
        originalSpriteColor = spriteRenderer.color;
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0)
        {
            return; // Already dead
        }

        float actualDamage = amount;

        if (isCurrentlyStaggered)
        {
            actualDamage *= enemyData.staggerDamageMultiplier; // Apply riposte multiplier
            Debug.LogWarning(enemyData.enemyName + " is STAGGERED! Riposte damage: " + actualDamage + " (Original: " + amount + ")");
        }

        currentHealth -= actualDamage;
        Debug.Log(enemyData.enemyName + " took " + amount + " damage. Current health: " + currentHealth);

        if (spriteRenderer != null)
        {
            StartCoroutine(HitFlash());
        }


        if (currentHealth <= 0)
        {
            currentHealth = 0; 
            Die();
        }
    }
    IEnumerator HitFlash()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (spriteRenderer != null) // Check if still exists (might have been destroyed/disabled)
        {
            spriteRenderer.color = originalSpriteColor;
        }
    }
    public void SetStaggered(bool state)
    {
        isCurrentlyStaggered = state;
        if (isCurrentlyStaggered)
        {
            Debug.Log(enemyData.enemyName + " has been set to STAGGERED state.");

        }
        else
        {
            Debug.Log(enemyData.enemyName + " is no longer staggered.");

        }
    }

    void Die()
    {
        Debug.Log(enemyData.enemyName + " has died.");
        spriteRenderer.color = Color.yellow;

        gameObject.SetActive(false);
        ReturnToPool();
    }
    /*void SpawnResources()
    {
        if (enemyData.healthPickupPrefab != null && Random.value < enemyData.healthDropChance) // Example: 50% chance
        {
            Instantiate(enemyData.healthPickupPrefab, transform.position, Quaternion.identity);
            Debug.Log(enemyData.enemyName + " dropped health.");
        }
        if (enemyData.ammoPickupPrefab != null && Random.value < enemyData.ammoDropChance) // Example: 30% chance
        {
            Instantiate(enemyData.ammoPickupPrefab, transform.position + Vector3.right * 0.5f, Quaternion.identity); // Offset slightly
            Debug.Log(enemyData.enemyName + " dropped ammo.");
        }
    }*/
    // IEnumerator DieWithAnimation()
    // {
    //     // if (animator) animator.SetTrigger("Die");
    //     // yield return new WaitForSeconds(deathAnimationDuration); // Wait for animation
    //     SpawnResources();
    //     gameObject.SetActive(false);
    // }
    // You might want to add a reference to the player later for AI
    // private Transform _playerTransform;
    // public void SetPlayerReference(Transform player) { _playerTransform = player; }
    private void ReturnToPool()
    {
        if (_poolInfo != null)
        {
            _poolInfo.ReleaseToPool();
        }
        else
        {

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Release(this.gameObject);
            }
            else
            {
                Debug.LogWarning($"No PoolManager instance or PoolableObjectInfo found for {gameObject.name}. Destroying.", this);
                Destroy(gameObject);
            }
        }
    }
}