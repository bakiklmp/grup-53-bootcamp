using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ProjectileSpawnData
{
    [Tooltip("The projectile prefab (must have a Projectile.cs script).")]
    public GameObject projectilePrefab;

    [Tooltip("Offset from the enemy's attack pivot/facing direction to spawn projectile(s).")]
    public Vector2 spawnPointOffset = Vector2.up; // Example: 1 unit in front

    public ProjectilePatternType patternType = ProjectilePatternType.Single;

    [Header("Pattern Specific Parameters")]
    [Tooltip("Number of projectiles for Spread, Volley, CircleBurst.")]
    public int numberOfProjectiles = 1;

    [Tooltip("Total angle for Spread (degrees). Delay between shots for Volley (seconds)." +
             "For CircleBurst, can be initial angle offset. " +
             "For Spiral, can be degrees per second of rotation or speed of expansion.")]
    public float patternParameter1 = 30f; // Spread Angle or Volley Delay or Circle Initial Angle Offset or Spiral Rotation Speed

    [Tooltip("For Spiral: Angle step per projectile. For CircleBurst, can be time between bursts if numberOfProjectiles > 1 indicates bursts. For other future patterns.")]
    public float patternParameter2 = 10f; // Spiral Angle Step or CircleBurst Time Between Bursts

    [Header("Projectile Properties")]
    public float projectileSpeed = 10f;
    public float projectileDamage = 10f;
    public float projectileLifetime = 5f;

    [Tooltip("Default parryability for all projectiles spawned by this data. Can be overridden by 'Override Default Parryability' below.")]
    public bool isProjectileParryableByPlayerHitbox = false; // Can player's parry action destroy/deflect this?

    [Header("Parryability Override (for multi-projectile patterns)")]
    [Tooltip("If true, the 'Parryability Sequence' below will be used to determine parryability for each projectile in a sequence " +
             "(e.g., for Volley or Spread) instead of the single 'Is Projectile Parryable' bool above.")]
    public bool overrideDefaultParryability = false;

    [Tooltip("Sequence of true (parryable) / false (unparryable) for projectiles. " +
             "Cycles if the sequence is shorter than Number Of Projectiles. " +
             "Only used if 'Override Default Parryability' is true. " +
             "Example for a 5-shot volley where 1st & 4th are parryable: [true, false, false, true, false]")]
    public List<bool> parryabilitySequence = new List<bool>();



    // public List<StatusEffectData> statusEffectsToApplyOnHit; // For later
}