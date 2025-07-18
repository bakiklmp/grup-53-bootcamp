using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Info")]
    public string enemyName = "Charger Grunt"; // For UI or debugging

    [Header("Stats")]
    public float maxHealth = 50f;
    public float movementSpeed = 3f;

    [Header("Combat")]
    public float meleeAttackDamage = 10f;  
    public float meleeAttackRange = 1.5f;  
    public float meleeAttackCooldown = 2f;
    public float attackTelegraphTime = 0.2f; // Time for the telegraph before actual hit / for parry

    public bool meleeAttackIsParryable = true;

    [Header("Stagger Mechanics")]
    public float staggerDuration = 2.5f;
    public float staggerDamageMultiplier = 2.0f; // Takes double damage when staggered

    /*[Header("Resource Drops")]
    public GameObject healthPickupPrefab;
    [Range(0f, 1f)] public float healthDropChance = 0.5f;
    public GameObject ammoPickupPrefab;
    [Range(0f, 1f)] public float ammoDropChance = 0.3f;*/

    // Future additions could go here:
    // public GameObject visualPrefab; // If  spawn a generic shell and then attach visuals
    // public AudioClip attackSound;
    // public GameObject deathParticles;
    // public LootTable lootTable;
}