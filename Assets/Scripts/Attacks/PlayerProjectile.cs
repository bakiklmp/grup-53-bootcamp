using UnityEngine;

public class PlayerProjectile : ProjectileBase
{
    public override void InitializeProjectile(Vector2 direction, float projectileSpeed, float projectileDamage, float projectileLifetime,string projectileTargetTag , string projectileOwnerTag)
    {
        base.InitializeProjectile(direction, projectileSpeed, projectileDamage, projectileLifetime, projectileTargetTag, projectileOwnerTag);
    }


    public override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D (other);

        /*if (other.CompareTag(ownerTag) || other.CompareTag(gameObject.tag)) // Assuming projectiles also have a "Projectile" tag
        {
            // Debug.Log("Projectile hit its owner or another projectile. Ignoring.");
            return;
        }*/

        // Right now only target is enemy, later on different enemies could have different tags
        if (other.CompareTag(targetTag))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                Debug.Log($"Projectile hit Player for {damage} damage.");
                enemy.TakeDamage(damage); // 'false' for wasPlayerBlocking
            }

            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            ReturnToPool();
            return;
        }

       /* if (other.gameObject.layer == LayerMask.NameToLayer("Environment") || other.CompareTag("Wall")) // Example layer/tag
        {
            // Debug.Log("Projectile hit environment.");
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            ReturnToPool();
            return;
        }*/
    }
}
