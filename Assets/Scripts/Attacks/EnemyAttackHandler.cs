using System.Collections;
using System.Collections.Generic; 
using UnityEngine;

[RequireComponent(typeof(EnemyBase))] 
public class EnemyAttackHandler : MonoBehaviour
{
    public bool isAttacking { get; private set; }

    private EnemyBase enemyBase;
    private Coroutine currentAttackCoroutine;
    private Dictionary<AttackData, float> attackCooldowns = new Dictionary<AttackData, float>();

    private ChargerFSM_AI enemyAIController; // Enemy specific AI State/Behaviour script
    private PlayerCombatController playerCombatController; // Cache player combat controller
    private AttackData currentAttackDataForGizmoDisplay;
    private EnemySpecialAbilityRelay enemySpecialAbilityRelay;
    private EnemyMovementController enemyMovementController;

    [Header("Colors For visualiusing attack")]
    public Color generalTelegraphColor = new Color(1f, 0.8f, 0.4f, 1f); // Yellowish/Orange
    public Color hitboxActiveColor = new Color(1f, 0.4f, 0.4f, 1f);   // Light Red
    public Color parryableWindowColor = new Color(0.4f, 1f, 0.4f, 1f); // Light Green

    private SpriteRenderer enemySpriteRenderer;
    private Color originalEnemySpriteColor;

    private bool wasAttackInterruptedByParry; // Flag to break out of phase loop if parried

    void Awake()
    {
        enemyBase = GetComponent<EnemyBase>();
        enemyAIController = GetComponent<ChargerFSM_AI>();
        enemySpecialAbilityRelay = GetComponent<EnemySpecialAbilityRelay>();
        enemyMovementController = GetComponent<EnemyMovementController>();
        if (enemyMovementController == null)
        {
            Debug.LogError(gameObject.name + ": EnemyAttackHandler requires an EnemyMovementController component. Movement overrides will not work.");
        }
        if (enemySpecialAbilityRelay == null)
        {
            Debug.LogWarning(gameObject.name + ": EnemySpecialAbilityRelay does not exist");

        }

        if (enemyAIController == null)
        {
            Debug.LogWarning(gameObject.name + ": EnemyAttackHandler could not find an AI Controller (e.g., ChargerFSM_AI). Stagger on parry might not work.");
        }

        // Get SpriteRenderer and original color
        if (enemyBase.spriteRenderer != null) // Assuming EnemyBase has a public SpriteRenderer reference
        {
            enemySpriteRenderer = enemyBase.spriteRenderer;
            originalEnemySpriteColor = enemySpriteRenderer.color;
        }
        else
        {
            Debug.LogWarning(gameObject.name + ": EnemyAttackHandler could not find SpriteRenderer via EnemyBase. Color feedback will not work.");
        }
    }
    void Start() // It's better to find player components in Start, after all Awakes have run
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerCombatController = playerObject.GetComponent<PlayerCombatController>();
            if (playerCombatController == null)
            {
                Debug.LogError("EnemyAttackHandler: PlayerCombatController not found on Player! Parry/Block checks will fail.");
            }
        }
        else
        {
            Debug.LogError("EnemyAttackHandler: Player object not found. Attack functionality will be impaired.");
        }
    }

    void Update()
    {
        // Update cooldowns
        if (attackCooldowns.Count > 0)
        {
            List<AttackData> keys = new List<AttackData>(attackCooldowns.Keys);
            foreach (AttackData attack in keys)
            {
                if (attackCooldowns[attack] > 0)
                {
                    attackCooldowns[attack] -= Time.deltaTime;
                }
                else
                {
                    attackCooldowns[attack] = 0; // Clamp at zero
                }
            }
        }
    }

    public bool IsAttackOnCooldown(AttackData attackData)
    {
        return attackCooldowns.ContainsKey(attackData) && attackCooldowns[attackData] > 0;
    }

    public bool StartAttackExecution(AttackData attackToRun)
    {
        if (isAttacking)
        {
            Debug.LogWarning(gameObject.name + " tried to start attack '" + attackToRun.attackName + "' while already attacking.");
            return false; // Already performing an attack
        }

        if (IsAttackOnCooldown(attackToRun))
        {
            Debug.Log(gameObject.name + " tried to use attack '" + attackToRun.attackName + "' but it's on cooldown.");
            return false; // Attack is on cooldown
        }

        if (attackToRun == null || attackToRun.phases.Count == 0)
        {
            Debug.LogError(gameObject.name + " tried to execute an invalid or empty AttackData.");
            return false;
        }

        isAttacking = true;
        wasAttackInterruptedByParry = false; // Reset parry interruption flag for this new attack sequence
        currentAttackDataForGizmoDisplay = attackToRun;
        currentAttackCoroutine = StartCoroutine(ExecuteAttackPhases(attackToRun));
        Debug.Log(gameObject.name + " started attack: " + attackToRun.attackName);
        return true;
    }

    public void InterruptAttack(bool byParry = false)
    {
        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
            Debug.Log(gameObject.name + " attack " + (byParry ? "INTERRUPTED BY PARRY." : "interrupted."));
        }
        // Reset any attack-specific states here if needed (e.g., movement overrides)
        isAttacking = false;
        if (byParry)
        {
            wasAttackInterruptedByParry = true; // Ensure AI knows it was a parry that stopped it
            // Stagger is handled by AI component when notified
        }
        if (enemySpriteRenderer != null)
        {
            enemySpriteRenderer.color = originalEnemySpriteColor; // Reset color on interrupt
        }
        if (enemyMovementController != null && enemyMovementController.IsOverrideActive) // Ensure check
        {
            enemyMovementController.StopMovementOverride(true); // Play end effects
        }
    }

    private IEnumerator ExecuteAttackPhases(AttackData currentAttackData)
    {
        if (playerCombatController != null)
        {
            Vector3 directionToPlayer = (playerCombatController.transform.position - transform.position);
            Vector2 direction2D = new Vector2(directionToPlayer.x, directionToPlayer.y).normalized;
            if (direction2D != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction2D.y, direction2D.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }
        }

        foreach (AttackPhaseData phase in currentAttackData.phases)
        {
            if (wasAttackInterruptedByParry) break;

            Debug.LogFormat(gameObject.name + " STARTING Phase: '{0}' (Attack: '{1}', Duration: {2}s, Action: {3}, MoveOverride {4})",
                phase.phaseName, currentAttackData.attackName, phase.duration, phase.actionType, phase.movementOverride.movementType);


            if (enemyMovementController != null) // Check if it exists
            {
                if (phase.movementOverride.movementType != MovementOverrideType.None)
                {
                    Transform targetForLunge = null;
                    if (phase.movementOverride.movementType == MovementOverrideType.LungeToTarget)
                    {
                        if (playerCombatController != null) targetForLunge = playerCombatController.transform;
                    }
                    enemyMovementController.StartMovementOverride(phase.movementOverride, targetForLunge, transform);
                }
                else if (enemyMovementController.IsOverrideActive) 
                {
                    enemyMovementController.StopMovementOverride(true); // Play end effects for the stopped override
                }
            }

            if (!string.IsNullOrEmpty(phase.animationTriggerName)) { Debug.Log($"-- Phase '{phase.phaseName}': Would trigger Animation: {phase.animationTriggerName}"); }
            if (phase.soundEffect != null) { Debug.Log($"-- Phase '{phase.phaseName}': Would play Sound: {phase.soundEffect.name}"); }

            switch (phase.actionType)
            {
                case AttackActionType.Projectile:
                    if (phase.projectileSpawnSettings != null && phase.projectileSpawnSettings.projectilePrefab != null)
                    {
                        switch (phase.projectileSpawnSettings.patternType)
                        {
                            case ProjectilePatternType.Single:
                                SpawnSingleProjectile(phase, phase.projectileSpawnSettings);
                                break;
                            case ProjectilePatternType.Spread:
                                SpawnSpreadPattern(phase, phase.projectileSpawnSettings);
                                break;
                            case ProjectilePatternType.Volley:
                                yield return StartCoroutine(SpawnVolleyPattern(phase, phase.projectileSpawnSettings));
                                break;
                            case ProjectilePatternType.CircleBurst:
                                yield return StartCoroutine(SpawnCircleBurstPattern(phase, phase.projectileSpawnSettings));
                                break;
                            case ProjectilePatternType.Spiral:
                                yield return StartCoroutine(SpawnSpiralPattern(phase, phase.projectileSpawnSettings));
                                break;
                            default:
                                Debug.LogWarning($"Phase '{phase.phaseName}': Projectile pattern type '{phase.projectileSpawnSettings.patternType}' not handled by direct call.");
                                LogPhaseAction(phase);
                                break;
                        }
                    }
                    else { /* Error log */ }
                    break;

                case AttackActionType.SpawnEnemy: 
                    if (phase.enemySpawnSettings != null)
                    {
                        yield return StartCoroutine(SpawnEnemiesAction(phase, phase.enemySpawnSettings));
                    }
                    else
                    {
                        Debug.LogError($"Phase '{phase.phaseName}': SpawnEnemy action selected but EnemySpawnSettings is null.");
                    }
                    break;
                case AttackActionType.EffectOnly:
                    if(phase.visualEffectPrefabOnSelf != null)
                    {
                        GameObject vfxInstance = Instantiate(phase.visualEffectPrefabOnSelf, transform.position, transform.rotation, transform);
                    }
                    LogPhaseAction(phase);
                    break;
                case AttackActionType.CustomScriptedEvent:
                    if (enemySpecialAbilityRelay != null && !string.IsNullOrEmpty(phase.customEventName))
                    {
                        enemySpecialAbilityRelay.HandleCustomEvent(phase.customEventName);
                    }
                    else if (string.IsNullOrEmpty(phase.customEventName))
                    {
                        Debug.LogWarning($"Phase '{phase.phaseName}': CustomScriptedEvent action selected, but customEventName is empty.");
                    }
                    else
                    {
                        Debug.LogWarning($"Phase '{phase.phaseName}': CustomScriptedEvent '{phase.customEventName}' triggered, but no EnemySpecialAbilityRelay found on {gameObject.name}.");
                    }
                    break;
            }
           
            float phaseTimer = 0f;
            bool isMeleeHitboxCurrentlyActiveInLogic = false;
            List<Collider2D> hitTargetsThisPhaseActivation = new List<Collider2D>();

            while (phaseTimer < phase.duration)
            {
                if (wasAttackInterruptedByParry) break;
                phaseTimer += Time.deltaTime;

                Color colorToSetThisFrame = originalEnemySpriteColor; // Default color

                if (phase.actionType == AttackActionType.Melee && phase.meleeHitbox != null)
                {
                    bool isTimingForHitboxToBeActive = (phaseTimer >= phase.meleeHitbox.hitActiveStartTime &&
                                                       phaseTimer < (phase.meleeHitbox.hitActiveStartTime + phase.meleeHitbox.hitActiveDuration));

                    if (isTimingForHitboxToBeActive)
                    {
                        // Hitbox should be active based on timing.
                        if (!isMeleeHitboxCurrentlyActiveInLogic) // It wasn't active before, but it is NOW!
                        {
                            isMeleeHitboxCurrentlyActiveInLogic = true; // Mark it as logically active.
                            hitTargetsThisPhaseActivation.Clear();    // Clear the list for this new activation period.
                            Debug.Log($"-- Phase '{phase.phaseName}': MELEE HITBOX ACTIVATED (Logic)");
                        }

                        colorToSetThisFrame = hitboxActiveColor; // Default color for active hitbox
                        float timeIntoThisActiveHitboxWindow = phaseTimer - phase.meleeHitbox.hitActiveStartTime;

                        if (phase.isMeleeHitParryable)
                        {
                            bool isMomentVisuallyParryable =
                                (timeIntoThisActiveHitboxWindow >= phase.meleeParryWindowStartOffset &&
                                 timeIntoThisActiveHitboxWindow < (phase.meleeParryWindowStartOffset + phase.meleeParryWindowDuration));
                            if (isMomentVisuallyParryable)
                            {
                                colorToSetThisFrame = parryableWindowColor;
                            }
                        }
                        // Perform the actual hit detection logic
                        PerformMeleeHitDetection(phase, timeIntoThisActiveHitboxWindow, hitTargetsThisPhaseActivation);
                    }
                    else 
                    {
                        if (isMeleeHitboxCurrentlyActiveInLogic) 
                        {
                            isMeleeHitboxCurrentlyActiveInLogic = false; 
                            Debug.Log($"-- Phase '{phase.phaseName}': MELEE HITBOX DEACTIVATED (Logic)");
                        }
                        colorToSetThisFrame = generalTelegraphColor; 
                    }
                }
                else if (phase.actionType == AttackActionType.Wait ||
                         phase.actionType == AttackActionType.Projectile || 
                         phase.actionType == AttackActionType.SpawnEnemy ||
                         phase.actionType == AttackActionType.EffectOnly ||
                         phase.actionType == AttackActionType.CustomScriptedEvent)
                {
                    colorToSetThisFrame = generalTelegraphColor;
                }
                // else: color remains originalEnemySpriteColor


                if (enemySpriteRenderer != null)
                {
                    enemySpriteRenderer.color = colorToSetThisFrame;
                }

                yield return null; // Pause until next frame
            } 

            if (isMeleeHitboxCurrentlyActiveInLogic) // Ensure logical state is reset if phase ends mid-hitbox
            {
                isMeleeHitboxCurrentlyActiveInLogic = false;
                Debug.Log($"-- Phase '{phase.phaseName}': MELEE HITBOX DEACTIVATED (Phase End by Duration)");
            }
            if (enemySpriteRenderer != null) // Reset color after each phase to original
            {
                enemySpriteRenderer.color = originalEnemySpriteColor;
            }

            // Log details for actions whose primary effect wasn't continuous (like Melee)
            if (phase.actionType != AttackActionType.Melee)
            {
                LogPhaseAction(phase);
            }

            if (phase.endAttackSequenceAfterThisPhase || wasAttackInterruptedByParry)
            {
                Debug.Log(gameObject.name + " Attack Sequence '" + currentAttackData.attackName + "' ended.");
                break; // Exit the outer `foreach` loop
            }
        } 

        if (enemySpriteRenderer != null) enemySpriteRenderer.color = originalEnemySpriteColor; // Final safety reset
        isAttacking = false;
        if (enemyMovementController != null && enemyMovementController.IsOverrideActive) // Ensure check
        {
            enemyMovementController.StopMovementOverride(true); // Play end effects
        }
        if (!wasAttackInterruptedByParry)
        {
            attackCooldowns[currentAttackData] = currentAttackData.cooldownTime;
            Debug.Log(gameObject.name + " Attack '" + currentAttackData.attackName + "' on cooldown for " + currentAttackData.cooldownTime + "s.");
        }

        currentAttackCoroutine = null;
        currentAttackDataForGizmoDisplay = null;
    }

    private void PerformMeleeHitDetection(AttackPhaseData phase, float timeIntoActiveHitboxWindow, List<Collider2D> alreadyHitThisActivation)
    {

        if (playerCombatController == null) return; // Safety check

        Vector2 relativeOffset = phase.meleeHitbox.offset;
        Vector2 worldOffset = transform.rotation * relativeOffset;
        Vector2 hitboxCenter = (Vector2)transform.position + worldOffset;

        Collider2D[] hits;
        switch (phase.meleeHitbox.shape)
        {
            case HitboxShape.Circle:
                hits = Physics2D.OverlapCircleAll(hitboxCenter, phase.meleeHitbox.primarySize, LayerMask.GetMask("Player"));
                break;
            case HitboxShape.Box:
                float boxAngle = transform.eulerAngles.z;
                hits = Physics2D.OverlapBoxAll(hitboxCenter, new Vector2(phase.meleeHitbox.primarySize, phase.meleeHitbox.secondarySize), boxAngle, LayerMask.GetMask("Player"));
                break;
            default: return; // Unsupported shape
        }

        foreach (Collider2D hitCollider in hits)
        {
            if (alreadyHitThisActivation.Contains(hitCollider))
            {
                continue; 
            }

            PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>(); // Get player's health component

            if (playerHealth != null) 
            {
                bool parriedSuccessfully = false;

                if (phase.isMeleeHitParryable)
                {
                    if (playerCombatController.isParryAttemptActive)
                    {
                        bool isHitMomentWithinParryWindow =
                            (timeIntoActiveHitboxWindow >= phase.meleeParryWindowStartOffset &&
                             timeIntoActiveHitboxWindow < (phase.meleeParryWindowStartOffset + phase.meleeParryWindowDuration));

                        if (isHitMomentWithinParryWindow)
                        {
                            parriedSuccessfully = true;
                            Debug.LogWarning($"PLAYER PARRIED {gameObject.name}'s attack '{phase.phaseName}'!");

                            playerCombatController.NotifySuccessfulParry();

                            if (enemyAIController != null)
                            {
                                enemyAIController.BecomeStaggered();
                            }
                            InterruptAttack(byParry: true); 
                        }
                    }
                } 

                if (!parriedSuccessfully)
                {
                    Debug.Log($"MELEE HIT on {hitCollider.name}! Damage: {phase.meleeDamage}, Player Blocking: {playerCombatController.isBlocking}");
                    playerHealth.TakeDamage(phase.meleeDamage, playerCombatController.isBlocking);
                }

                alreadyHitThisActivation.Add(hitCollider);

            }
        } 
    }
    private void SpawnSingleProjectile(AttackPhaseData phase, ProjectileSpawnData spawnData)
    {
        if (spawnData.projectilePrefab == null)
        {
            Debug.LogError($"Phase '{phase.phaseName}': Projectile prefab is null in ProjectileSpawnData.");
            return;
        }

        Vector2 worldSpawnOffset = transform.rotation * spawnData.spawnPointOffset;
        Vector2 spawnPosition = (Vector2)transform.position + worldSpawnOffset;

        Vector2 fireDirection = transform.up; // Assuming sprite's 'up' is forward due to rotation

        GameObject projectileGO = PoolManager.Instance.Get(spawnData.projectilePrefab, spawnPosition, Quaternion.LookRotation(Vector3.forward, fireDirection)); // Get from pool and set initial rotation

        if (projectileGO != null)
        {
            EnemyProjectile projectileScript = projectileGO.GetComponent<EnemyProjectile>();
            if (projectileScript != null)
            {
                projectileScript.InitializeProjectile(
                    fireDirection,
                    spawnData.projectileSpeed,
                    spawnData.projectileDamage,
                    spawnData.projectileLifetime,
                    gameObject.tag,
                    "Player",
                    spawnData.isProjectileParryableByPlayerHitbox
                );
                //Debug.Log($"Spawned single projectile for phase '{phase.phaseName}'");
            }
            else
            {
                Debug.LogError($"Phase '{phase.phaseName}': Spawned projectile prefab '{spawnData.projectilePrefab.name}' is missing Projectile.cs script.");
                projectileGO.SetActive(false); // Return to pool if misconfigured
            }
        }
        else
        {
            //Pooler couldn't provide the object, usually pooler logs this.
            Debug.Log($"PoolManager couldn't provide {projectileGO}");
        }
    }
    private void SpawnSpreadPattern(AttackPhaseData phase, ProjectileSpawnData spawnData)
    {
        if (spawnData.projectilePrefab == null)
        {
            Debug.LogError($"Phase '{phase.phaseName}': Spread - Projectile prefab is null in ProjectileSpawnData.");
            return;
        }
        if (spawnData.numberOfProjectiles <= 0)
        {
            Debug.LogError($"Phase '{phase.phaseName}': Spread - Number of projectiles is {spawnData.numberOfProjectiles}, must be > 0.");
            return;
        }

        Vector2 worldSpawnOffset = transform.rotation * spawnData.spawnPointOffset;
        Vector2 spawnPosition = (Vector2)transform.position + worldSpawnOffset;
        Vector2 baseFireDirection = transform.up; // Enemy's current forward direction

        float totalSpreadAngle = spawnData.patternParameter1; // This is the total arc the spread will cover
        float angleStep = 0;

        if (spawnData.numberOfProjectiles > 1)
        {
            // (numberOfProjectiles - 1) because N projectiles create N-1 gaps between them
            angleStep = totalSpreadAngle / (spawnData.numberOfProjectiles - 1);
        }

        float currentAngle = (spawnData.numberOfProjectiles > 1) ? -totalSpreadAngle / 2f : 0f;

        for (int i = 0; i < spawnData.numberOfProjectiles; i++)
        {
            if (i > 0 && spawnData.numberOfProjectiles > 1) // No step for the first projectile
            {
                currentAngle += angleStep;
            }

            Quaternion projectileRotation = Quaternion.Euler(0, 0, currentAngle);
            Vector2 fireDirection = projectileRotation * baseFireDirection;

            bool actualParryable = spawnData.isProjectileParryableByPlayerHitbox; // Default
            if (spawnData.overrideDefaultParryability && spawnData.parryabilitySequence != null && spawnData.parryabilitySequence.Count > 0)
            {
                int sequenceIndex = i % spawnData.parryabilitySequence.Count;
                actualParryable = spawnData.parryabilitySequence[sequenceIndex];
            }

            // Get projectile from pool and set its initial rotation to match fireDirection
            GameObject projectileGO = PoolManager.Instance.Get(spawnData.projectilePrefab, spawnPosition, Quaternion.LookRotation(Vector3.forward, fireDirection));

            if (actualParryable)
            {
                projectileGO.GetComponent<SpriteRenderer>().color = Color.black;
            }
            if (projectileGO != null)
            {
                EnemyProjectile projectileScript = projectileGO.GetComponent<EnemyProjectile>();
                if (projectileScript != null)
                {
                    // Assuming your InitializeProjectile method is:
                    // (Vector2 direction, float speed, float damage, float lifetime, string ownerTag, string targetTag, bool isParryable)
                    projectileScript.InitializeProjectile(
                        fireDirection,
                        spawnData.projectileSpeed,
                        spawnData.projectileDamage,
                        spawnData.projectileLifetime,
                        gameObject.tag, // Enemy's tag
                        "Player",       // Target tag
                        actualParryable // Use the determined parryability
                    );
                    // Debug.Log($"Spread shot {i + 1}/{spawnData.numberOfProjectiles} fired towards {fireDirection}. Parryable: {actualParryable}");
                }
                else
                {
                    Debug.LogError($"Phase '{phase.phaseName}': Spread - Spawned projectile prefab '{spawnData.projectilePrefab.name}' is missing EnemyProjectile script.");
                    // PoolManager.Instance.Return(projectileGO); // Or your pooler's equivalent
                    projectileGO.SetActive(false);
                }
            }
        }
    }
    private IEnumerator SpawnVolleyPattern(AttackPhaseData phase, ProjectileSpawnData spawnData)
    {
        if (spawnData.projectilePrefab == null)
        {
            Debug.LogError($"Phase '{phase.phaseName}': Volley - Projectile prefab is null.");
            yield break; // Exit coroutine
        }
        if (spawnData.numberOfProjectiles <= 0)
        {
            Debug.LogWarning($"Phase '{phase.phaseName}': Volley - numberOfProjectiles is 0 or less. No projectiles will spawn.");
            yield break;
        }

        Vector2 worldSpawnOffset = transform.rotation * spawnData.spawnPointOffset;
        Vector2 spawnPosition = (Vector2)transform.position + worldSpawnOffset;
        Vector2 baseFireDirection = transform.up; // Enemy's forward
        float delayBetweenShots = spawnData.patternParameter1; // patternParameter1 is used as delay for Volley

        for (int i = 0; i < spawnData.numberOfProjectiles; i++)
        {
            // Determine actual parryability for this specific projectile in the volley
            bool actualParryable = spawnData.isProjectileParryableByPlayerHitbox; // Default
            if (spawnData.overrideDefaultParryability && spawnData.parryabilitySequence != null && spawnData.parryabilitySequence.Count > 0)
            {
                int sequenceIndex = i % spawnData.parryabilitySequence.Count;
                actualParryable = spawnData.parryabilitySequence[sequenceIndex];
            }

            GameObject projectileGO = PoolManager.Instance.Get(spawnData.projectilePrefab, spawnPosition, Quaternion.LookRotation(Vector3.forward, baseFireDirection));

            if (actualParryable)
            {
                projectileGO.GetComponent<SpriteRenderer>().color = Color.black;
            }
            if (projectileGO != null)
            {
                EnemyProjectile projectileScript = projectileGO.GetComponent<EnemyProjectile>();
                if (projectileScript != null)
                {
                    projectileScript.InitializeProjectile(
                        baseFireDirection, // All volley shots go in the same base direction
                        spawnData.projectileSpeed,
                        spawnData.projectileDamage,
                        spawnData.projectileLifetime,
                        gameObject.tag,
                        "Player",
                        actualParryable // Use the determined parryability
                    );
                    // Debug.Log($"Volley shot {i + 1}/{spawnData.numberOfProjectiles} fired. Parryable: {actualParryable}");
                }
                else
                {
                    Debug.LogError($"Phase '{phase.phaseName}': Volley - Spawned projectile prefab '{spawnData.projectilePrefab.name}' is missing Projectile.cs script.");
                    projectileGO.SetActive(false);
                }
            }

            // If not the last shot, wait for the delay
            if (i < spawnData.numberOfProjectiles - 1)
            {
                if (delayBetweenShots > 0)
                {
                    yield return new WaitForSeconds(delayBetweenShots);
                }
                // If delay is 0, all shots will fire in very quick succession (potentially same frame if loop is fast enough)
            }
        }
        Debug.Log($"Phase '{phase.phaseName}': Volley of {spawnData.numberOfProjectiles} projectiles complete.");
    }
    private IEnumerator SpawnCircleBurstPattern(AttackPhaseData phase, ProjectileSpawnData spawnData)
    {
        if (spawnData.projectilePrefab == null)
        {
            Debug.LogError($"Phase '{phase.phaseName}': CircleBurst - Projectile prefab is null.");
            yield break; // Exit coroutine
        }
        if (spawnData.numberOfProjectiles <= 0)
        {
            Debug.LogWarning($"Phase '{phase.phaseName}': CircleBurst - numberOfProjectiles is {spawnData.numberOfProjectiles}. No projectiles will spawn.");
            yield break;
        }

        Vector2 worldSpawnOffset = transform.rotation * spawnData.spawnPointOffset;
        Vector2 spawnPosition = (Vector2)transform.position + worldSpawnOffset;

        float angleStep = 360f / spawnData.numberOfProjectiles;
        float initialAngleOffset = spawnData.patternParameter1; // patternParameter1 used as initial angle offset

        for (int i = 0; i < spawnData.numberOfProjectiles; i++)
        {
            float currentAngle = (i * angleStep) + initialAngleOffset;

            // Convert angle to a direction vector
            // Angle 0 is typically to the right (Vector2.right). We rotate Vector2.up as our base "forward" for consistency with other patterns.
            Quaternion rotation = Quaternion.Euler(0, 0, currentAngle);
            Vector2 fireDirection = rotation * Vector2.up; // Assuming projectiles are designed to fire "up" locally before rotation

            bool actualParryable = spawnData.isProjectileParryableByPlayerHitbox; // Default
            if (spawnData.overrideDefaultParryability && spawnData.parryabilitySequence != null && spawnData.parryabilitySequence.Count > 0)
            {
                int sequenceIndex = i % spawnData.parryabilitySequence.Count;
                actualParryable = spawnData.parryabilitySequence[sequenceIndex];
            }

            GameObject projectileGO = PoolManager.Instance.Get(spawnData.projectilePrefab, spawnPosition, Quaternion.LookRotation(Vector3.forward, fireDirection));

            if (actualParryable)
            {
                projectileGO.GetComponent<SpriteRenderer>().color = Color.black;
            }
            if (projectileGO != null)
            {
                EnemyProjectile projectileScript = projectileGO.GetComponent<EnemyProjectile>();
                if (projectileScript != null)
                {
                    projectileScript.InitializeProjectile(
                        fireDirection,
                        spawnData.projectileSpeed,
                        spawnData.projectileDamage,
                        spawnData.projectileLifetime,
                        gameObject.tag, // Enemy's tag
                        "Player",       // Target tag
                        actualParryable // Use the determined parryability
                    );
                }
                else
                {
                    Debug.LogError($"Phase '{phase.phaseName}': CircleBurst - Spawned projectile prefab '{spawnData.projectilePrefab.name}' is missing EnemyProjectile script.");
                    projectileGO.SetActive(false);
                }
            }
        }
        Debug.Log($"Phase '{phase.phaseName}': CircleBurst of {spawnData.numberOfProjectiles} projectiles complete.");
        yield return null;
    }
    private IEnumerator SpawnSpiralPattern(AttackPhaseData phase, ProjectileSpawnData spawnData)
    {
        if (spawnData.projectilePrefab == null)
        {
            Debug.LogError($"Phase '{phase.phaseName}': Spiral - Projectile prefab is null.");
            yield break;
        }
        if (spawnData.numberOfProjectiles <= 0)
        {
            Debug.LogWarning($"Phase '{phase.phaseName}': Spiral - numberOfProjectiles is {spawnData.numberOfProjectiles}. No projectiles will spawn.");
            yield break;
        }

        Vector2 worldSpawnOffset = transform.rotation * spawnData.spawnPointOffset;
        Vector2 spawnPosition = (Vector2)transform.position + worldSpawnOffset;

        float delayBetweenShots = spawnData.patternParameter1; // patternParameter1 used as delay between shots
        float angleStepPerShot = spawnData.patternParameter2;  // patternParameter2 used as angle step
        float currentSpiralAngle = 0f; // Initial angle for the spiral (could be an offset from enemy facing or absolute)

        Vector2 initialFacingForSpiral = transform.up; // The direction the spiral "starts" rotating from

        for (int i = 0; i < spawnData.numberOfProjectiles; i++)
        {
            // Rotate the initial facing direction by the current spiral angle
            Quaternion rotation = Quaternion.Euler(0, 0, currentSpiralAngle);
            Vector2 fireDirection = rotation * initialFacingForSpiral;

            bool actualParryable = spawnData.isProjectileParryableByPlayerHitbox; // Default
            if (spawnData.overrideDefaultParryability && spawnData.parryabilitySequence != null && spawnData.parryabilitySequence.Count > 0)
            {
                int sequenceIndex = i % spawnData.parryabilitySequence.Count;
                actualParryable = spawnData.parryabilitySequence[sequenceIndex];
            }

            //GameObject projectileGO = PoolManager.Instance.Get(spawnData.projectilePrefab, spawnPosition, Quaternion.identity);
            GameObject projectileGO = PoolManager.Instance.Get(spawnData.projectilePrefab, spawnPosition, Quaternion.LookRotation(Vector3.forward, fireDirection));

            if (actualParryable)
            {
                projectileGO.GetComponent<SpriteRenderer>().color = Color.black;
            }
            if (projectileGO != null)
            {
                EnemyProjectile projectileScript = projectileGO.GetComponent<EnemyProjectile>();
                if (projectileScript != null)
                {
                    projectileScript.InitializeProjectile(
                        fireDirection,
                        spawnData.projectileSpeed,
                        spawnData.projectileDamage,
                        spawnData.projectileLifetime,
                        gameObject.tag,
                        "Player",
                        actualParryable
                    );
                }
                else
                {
                    Debug.LogError($"Phase '{phase.phaseName}': Spiral - Spawned projectile prefab '{spawnData.projectilePrefab.name}' is missing EnemyProjectile script.");
                    projectileGO.SetActive(false);
                }
            }

            currentSpiralAngle += angleStepPerShot;

            if (i < spawnData.numberOfProjectiles - 1)
            {
                if (delayBetweenShots > 0)
                {
                    yield return new WaitForSeconds(delayBetweenShots);
                }
            }
        }
        Debug.Log($"Phase '{phase.phaseName}': Spiral of {spawnData.numberOfProjectiles} projectiles complete.");
    }
    private IEnumerator SpawnEnemiesAction(AttackPhaseData phase, EnemySpawnData spawnData)
    {
        if (spawnData.enemyPrefabToSpawn == null)
        {
            Debug.LogError($"Phase '{phase.phaseName}': SpawnEnemy - enemyPrefabToSpawn is null.");
            yield break; // Exit this coroutine
        }
        if (spawnData.numberOfEnemiesToSpawn <= 0)
        {
            Debug.LogWarning($"Phase '{phase.phaseName}': SpawnEnemy - numberOfEnemiesToSpawn is {spawnData.numberOfEnemiesToSpawn}. No enemies will spawn.");
            yield break;
        }

        Vector2 baseWorldSpawnPoint = (Vector2)transform.position + (Vector2)(transform.rotation * spawnData.spawnPositionOffset);

        Debug.Log($"Phase '{phase.phaseName}': Spawning {spawnData.numberOfEnemiesToSpawn} of '{spawnData.enemyPrefabToSpawn.name}'. Base point: {baseWorldSpawnPoint}");

        for (int i = 0; i < spawnData.numberOfEnemiesToSpawn; i++)
        {
            Vector2 finalSpawnPosition = baseWorldSpawnPoint;
            if (spawnData.numberOfEnemiesToSpawn > 1 && spawnData.spawnAreaRadius > 0)
            {
                finalSpawnPosition += Random.insideUnitCircle * spawnData.spawnAreaRadius;
            }

            GameObject spawnedEnemyGO = PoolManager.Instance.Get(spawnData.enemyPrefabToSpawn, finalSpawnPosition, Quaternion.identity);

            if (spawnedEnemyGO != null)
            {
                var aiComponent = spawnedEnemyGO.GetComponent<ChargerFSM_AI>(); // Or your base AI class/interface
                if (aiComponent != null)
                {
                    aiComponent.enabled = false;
                }
                StartCoroutine(DelayedActivateEnemy(spawnedEnemyGO, spawnData.activationDelay, phase.phaseName));
            }
        }

        yield return null;
    }

    private IEnumerator DelayedActivateEnemy(GameObject enemyToActivate, float delay, string spawningPhaseName)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        if (enemyToActivate != null && enemyToActivate.activeInHierarchy) // Check if it wasn't destroyed/returned to pool
        {
            var aiComponent = enemyToActivate.GetComponent<ChargerFSM_AI>(); // Replace with your base AI component type
            if (aiComponent != null)
            {
                aiComponent.enabled = true;
                Debug.Log($"Enemy '{enemyToActivate.name}' (spawned by phase '{spawningPhaseName}') AI activated after delay.");
            }
            else
            {
                Debug.LogWarning($"Enemy '{enemyToActivate.name}' (spawned by phase '{spawningPhaseName}') has no AI component to activate.");
            }

            EnemyBase enemyBase = enemyToActivate.GetComponent<EnemyBase>();
            if (enemyBase != null && enemyBase.enemyData != null)
            {
            }
        }
        else
        {
            Debug.LogWarning($"Tried to activate enemy (from phase '{spawningPhaseName}') after delay, but it was null or inactive.");
        }
    }

    private void LogPhaseAction(AttackPhaseData phase)
    {
        switch (phase.actionType)
        {
            case AttackActionType.Melee:
                break;
            case AttackActionType.Projectile:
                if (phase.projectileSpawnSettings != null /* && ... check if pattern not directly handled ... */)
                { /* ... log ... */ }
                break;
            case AttackActionType.SpawnEnemy:
                // Already logged by direct call if successful, this could be a fallback
                // Debug.Log($"-- Phase '{phase.phaseName}': Summary log for SPAWN ENEMY.");
                break;
            case AttackActionType.EffectOnly: // <-- NEW
                Debug.Log($"-- Phase '{phase.phaseName}': EffectOnly action occurred (VFX: {phase.visualEffectPrefabOnSelf?.name}).");
                break;
            case AttackActionType.Wait:
                Debug.Log($"-- Phase '{phase.phaseName}': Performing WAIT action (duration).");
                break;
            case AttackActionType.CustomScriptedEvent:
                Debug.Log($"-- Phase '{phase.phaseName}': CustomScriptedEvent '{phase.customEventName}' was processed.");
                break;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (currentAttackDataForGizmoDisplay == null || currentAttackDataForGizmoDisplay.phases == null)
        {
            return; // No current attack data to display Gizmos for
        }

        // Store original Gizmos matrix and color
        Matrix4x4 originalMatrix = Gizmos.matrix;
        Color originalColor = Gizmos.color;

        foreach (AttackPhaseData phase in currentAttackDataForGizmoDisplay.phases)
        {
            if (phase.actionType == AttackActionType.Melee && phase.meleeHitbox != null)
            {
                HitboxDefinitionData hitboxDef = phase.meleeHitbox;

                Vector2 relativeOffset = hitboxDef.offset;
                Vector2 worldOffset = transform.rotation * relativeOffset;
                Vector2 hitboxCenter = (Vector2)transform.position + worldOffset;

                Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Red, slightly transparent for potential hitboxes


                switch (hitboxDef.shape)
                {
                    case HitboxShape.Circle:
                        Gizmos.DrawSphere(hitboxCenter, hitboxDef.primarySize);
                        break;
                    case HitboxShape.Box:
                        Matrix4x4 rotationMatrix = Matrix4x4.TRS(hitboxCenter, transform.rotation, Vector3.one);
                        Gizmos.matrix = rotationMatrix;
                        Gizmos.DrawWireCube(Vector3.zero, new Vector3(hitboxDef.primarySize, hitboxDef.secondarySize, 0.1f));
                        Gizmos.matrix = originalMatrix; // Reset matrix
                        break;
                    case HitboxShape.Arc:
                        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange for arc
                        Gizmos.DrawSphere(hitboxCenter, hitboxDef.primarySize); // Approximate with a circle for now
                        Debug.LogWarning("Gizmo for Arc hitbox shape is approximated with a circle.");
                        break;
                }
            }
        }

        Gizmos.matrix = originalMatrix;
        Gizmos.color = originalColor;
    }
}