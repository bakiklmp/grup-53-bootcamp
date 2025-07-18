using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class ProjectileBase : MonoBehaviour, IPoolable
{

    // Properities for spawner
    private protected float speed;
    private protected float damage;
    private protected float lifetime = 3f;
    private protected string ownerTag;
    private protected string targetTag;
    private protected bool isParryableByPlayer;

    // Interntal State
    private protected float currentLifetime;
    private protected Vector2 initialDirection;
    private protected PoolableObjectInfo _poolInfo;
    private protected Rigidbody2D rb;

    public GameObject hitEffectPrefab;
    public GameObject parryEffectPrefab;

    private SpriteRenderer SpriteRenderer;
    private Color originalColor;
    void Awake()
    {

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Projectile is missing a Rigidbody2D component!", this);
        }

        _poolInfo = GetComponent<PoolableObjectInfo>();
        if (_poolInfo == null)
        {
            Debug.LogWarning($"Projectile {name} is missing PoolableObjectInfo. It might not be properly pooled.", this);
        }

        SpriteRenderer = GetComponent<SpriteRenderer>();
        if (SpriteRenderer == null)
        {
            Debug.LogError("Projectile is missing a SpriteRenderer component!", this);
        }
        originalColor = SpriteRenderer.color;
    }
    public virtual void InitializeProjectile(Vector2 direction, float projectileSpeed,float projectileDamage, float projectileLifetime, string projectileOwnerTag, string projectileTargetTag)
    {
        // Any other launch-specific setup can go here
        initialDirection = direction.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        lifetime = projectileLifetime;
        ownerTag = projectileOwnerTag;
        targetTag = projectileTargetTag;

        currentLifetime = 0f;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, initialDirection);
        if (rb != null)
        {
            rb.linearVelocity = initialDirection * speed;
        }
    }
    public virtual void InitializeProjectile(Vector2 direction, float projectileSpeed, float projectileDamage, float projectileLifetime, string projectileOwnerTag, string projectileTargetTag, bool parryable)
    {
        initialDirection = direction.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        lifetime = projectileLifetime;
        ownerTag = projectileOwnerTag;
        targetTag = projectileTargetTag;
        isParryableByPlayer = parryable;


        currentLifetime = 0f;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, initialDirection);
        if (rb != null)
        {
            rb.linearVelocity = initialDirection * speed;
        }
    }

    public virtual void OnObjectSpawn()
    {

        Debug.Log($"{gameObject.name} spawned from pool. Position: {transform.position}");
        
    }

    public virtual void OnObjectDespawn()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Stop movement when returning to pool
            rb.angularVelocity = 0f;    // Stop rotation
        }

        Debug.Log($"{gameObject.name} despawned to pool.");
        SpriteRenderer.color = originalColor;
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return; 

        currentLifetime += Time.deltaTime;

        if (currentLifetime >= lifetime)
        {
            ReturnToPool();
            return;
        }
    }

    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(ownerTag) || other.CompareTag(gameObject.tag)) 
        {
            return;
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("Environment") || other.CompareTag("Wall")) 
        {
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            ReturnToPool();
            return;
        }
    }

    private protected void ReturnToPool()
    {
        if (_poolInfo != null)
        {
            _poolInfo.ReleaseToPool();
        }
        else
        {

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Release(gameObject);
            }
            else
            {
                Debug.LogWarning($"No PoolManager instance or PoolableObjectInfo found for {gameObject.name}. Destroying.", this);
                Destroy(gameObject);
            }
        }
    }
}