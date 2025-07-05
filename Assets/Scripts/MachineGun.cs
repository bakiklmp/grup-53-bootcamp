using UnityEngine;

public class MachineGun : Weapon
{
    [Header("Object To Spawn")]
    public GameObject projectilePrefab;


    void Start()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab not assigned in Machingun.", this);
            enabled = false;
            return;
        }

        if (PoolManager.Instance != null)
        {
            // The Get method with autoCreatePoolIfMissing = true 
            // Or
            // PoolManager.Instance.CreatePool(projectilePrefab, 10, 50);
        }
        else
        {
            Debug.LogError("PoolManager instance not found! Machingun cannot function.", this);
            enabled = false;
        }
    }
    protected override void PerformAttack(Vector2 aimPosition)
    {
        if (PoolManager.Instance == null || projectilePrefab == null)
        {
            Debug.LogWarning("Cannot spawn projectile: PoolManager, Prefab, or PlayerMovementController is missing.");
            return;
        }
        Vector2 fireDirection = aimPosition;

        GameObject projectileInstance = PoolManager.Instance.Get(
            projectilePrefab,
            transform.position,
            transform.rotation

        );

        if (projectileInstance != null)
        {
            PlayerProjectile projectileScript = projectileInstance.GetComponent<PlayerProjectile>();
            if (projectileScript != null)
            {
                projectileScript.InitializeProjectile(fireDirection, weaponData.projectileSpeed, weaponData.damage, weaponData.projectileLifetime, gameObject.tag, weaponData.projectileTargetTag);
            }
            else
            {
                Debug.LogError("Machingun object is missing Projectile script!", projectileInstance);
            }
            Debug.Log($"Machingun got {projectileInstance.name} from pool, launching towards {fireDirection}.");
        }
        else
        {
            Debug.LogWarning("Machingun failed to get a projectile from the pool (it might be at max capacity and not auto-creating, or prefab is null).");
        }
    }
}