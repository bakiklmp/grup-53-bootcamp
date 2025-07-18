using UnityEngine;

public class EnemyProjectile : ProjectileBase
{
    public override void InitializeProjectile(Vector2 direction, float projectileSpeed, float projectileDamage, float projectileLifetime, string projectileOwnerTag, string projectileTargetTag, bool parryable)
    {
        base.InitializeProjectile(direction, projectileSpeed, projectileDamage, projectileLifetime, projectileOwnerTag, projectileTargetTag, parryable);
    }

    public override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);


        if (other.CompareTag(targetTag))
        {
            PlayerCombatController playerCombatController = other.GetComponentInParent<PlayerCombatController>();
            if (playerCombatController != null)
            {
                Debug.Log($"{gameObject.name} can't find PlayerCombatController of {other.name}");
            }

            if (isParryableByPlayer && playerCombatController.isParryAttemptActive)
            {
                    Debug.LogWarning("PROJECTILE PARRIED by player!");
                    if (parryEffectPrefab != null) Instantiate(parryEffectPrefab, transform.position, Quaternion.identity);
                    playerCombatController.NotifySuccessfulParry();

                    ReturnToPool();
                    return;
            }
            else
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Debug.Log($"Projectile hit Player for {damage} damage.");
                    playerHealth.TakeDamage(damage, false); // 'false' for wasPlayerBlocking
                }

                if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                ReturnToPool();
                return;
            }       
        }


    }
}
