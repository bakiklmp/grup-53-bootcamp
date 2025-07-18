using UnityEngine;

public class ShotgunWeapon : Weapon 
{
    [Header("Shotgun Specific Stats")]
    [SerializeField] private int pelletsPerShot = 8;
    [SerializeField] private float spreadAngle = 15f;

    [Header("Damage Falloff")]
    [Tooltip("X-axis: distance (0=muzzle, 1=max range), Y-axis: damage multiplier (0 to 1)")]
    [SerializeField] private AnimationCurve damageFalloffCurve = AnimationCurve.Linear(0, 1, 1, 0.5f);

    [Header("References")]
    [SerializeField] private LayerMask hitMask;


    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    protected override void PerformAttack(Vector2 aimPosition)
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found for aiming!");
            mainCamera = Camera.main; 
            if (mainCamera == null) return;
        }


        for (int i = 0; i < pelletsPerShot; i++)
        {
            float currentPelletSpread = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
            Quaternion spreadRotation = Quaternion.Euler(0, 0, currentPelletSpread);
            Vector2 pelletDirection = spreadRotation * aimPosition; // Apply spread to the aiming direction

            // Perform the Raycast
            RaycastHit2D hitInfo = Physics2D.Raycast(transform.position, pelletDirection, weaponData.maxRaycastDistance, hitMask);


            if (hitInfo.collider != null)
            {
                Debug.DrawRay(transform.position, pelletDirection * hitInfo.distance, Color.red, 0.2f);

                float distanceRatio = hitInfo.distance / weaponData.maxRaycastDistance;
                float falloffMultiplier = damageFalloffCurve.Evaluate(distanceRatio);
                float actualDamage = weaponData.damage * falloffMultiplier; // Use weaponData.damage

                /*IDamageable damageable = hitInfo.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(actualDamage);
                }*/
                EnemyBase enemy = hitInfo.collider.GetComponent<EnemyBase>();
                if(enemy != null)
                {
                    enemy.TakeDamage(actualDamage);
                }


                // if (weaponData.hitEffectPrefab != null) Instantiate(weaponData.hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            }
            else
            {
                Debug.DrawRay(transform.position, pelletDirection * weaponData.maxRaycastDistance, Color.yellow, 0.2f);

            }
        }
    }
}