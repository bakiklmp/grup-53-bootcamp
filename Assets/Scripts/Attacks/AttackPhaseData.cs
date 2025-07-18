using UnityEngine;

[System.Serializable]
public class AttackPhaseData
{
    [Tooltip("Descriptive name for this phase (e.g., 'WindUp', 'Slash1', 'FireballVolley').")]
    public string phaseName = "New Attack Phase";

    [Tooltip("How long this phase lasts. If driven by animation, can be approximate or for fallback.")]
    public float duration = 0.5f; // The total time this specific segment of the attack (e.g., one swing)

    [Tooltip("Animator trigger to play when this phase starts.")]
    public string animationTriggerName;

    [Tooltip("Sound effect to play when this phase starts.")]
    public AudioClip soundEffect;

    [Tooltip("Defines specific movement during this phase, overriding default AI movement.")]
    public MovementOverrideData movementOverride; // Embed the class

    [Header("Primary Action of this Phase")]
    public AttackActionType actionType = AttackActionType.Wait;

    // Conditional fields based on actionType (Editor script can make this cleaner later)
    [Header("Melee Action Details (if ActionType is Melee)")]
    public HitboxDefinitionData meleeHitbox;
    public float meleeDamage = 10f;
    public bool isMeleeHitParryable = true;

    //This is an offset from the moment the hitbox becomes active.
    //If this is 0.0f, the parryable window starts at the same instant the hitbox becomes active.
    //If it's 0.05f, the parryable window starts 0.05 seconds after the hitbox becomes active.
    public float meleeParryWindowStartOffset = 0.0f;

    //Once the parryable window opens (after meleeParryWindowStartOffset from the hitbox activation time)
    //this is how long it stays open.
    public float meleeParryWindowDuration = 0.15f;

    [Header("Projectile Action Details (if ActionType is Projectile)")]
    public ProjectileSpawnData projectileSpawnSettings;

    [Header("Enemy Spawn Action Details (if ActionType is SpawnEnemy)")]
    public EnemySpawnData enemySpawnSettings;

    [Header("EffectOnly Action Details (if ActionType is EffectOnly)")]
    public GameObject visualEffectPrefabOnSelf; 
    // public List<StatusEffectData> statusEffectsToApplyToSelf;

    [Header("Custom Event Details (if ActionType is CustomScriptedEvent)")]
    public string customEventName; // Key for EnemySpecialAbilityRelay to listen for

    [Header("Phase Flow Control")]
    [Tooltip("If true, moves to the next phase without waiting for full duration (e.g., if animation event signals early completion).")]
    public bool chainToNextPhaseImmediately = false;

    [Tooltip("If true, the entire AttackData sequence ends after this phase.")]
    public bool endAttackSequenceAfterThisPhase = false;
}