using UnityEngine;

[RequireComponent(typeof(EnemyBase))]
[RequireComponent(typeof(EnemyAttackHandler))] 
public class ChargerFSM_AI : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chase,
        ExecutingAttackSequence, 
        Cooldown,
        Staggered
    }

    [Header("AI State")]
    public EnemyState currentState;

    [Header("AI Parameters (General)")]
    private float movementSpeed;
    private float engagementRangeForPrimaryAttack; // Derived from AttackData

    [Header("Detection Range")]
    public float detectionRange = 10f;
    public float loseSightRange = 15f;

    [Header("Attack Configuration")]
    [Tooltip("The primary AttackData this Charger uses for its melee attack.")]
    public AttackData primaryMeleeAttackData; 

    [Header("References")]
    private Transform playerTransform;
    private PlayerCombatController playerCombatController;
    private EnemyBase enemyBase;
    private EnemyAttackHandler attackHandler; 
    private EnemyMovementController movementController;

    [Header("Timers (AI specific, not for attack execution)")]
    private float staggerTimer;

    void Awake()
    {
        enemyBase = GetComponent<EnemyBase>();
        attackHandler = GetComponent<EnemyAttackHandler>(); // Get the attack handler
        movementController = GetComponent<EnemyMovementController>();
        if (movementController == null)
        {
            Debug.LogError(gameObject.name + ": ChargerFSM_AI requires an EnemyMovementController. AI movement will not work.");
            enabled = false;
            return;
        }
        if (enemyBase == null || enemyBase.enemyData == null || attackHandler == null)
        {
            Debug.LogError(gameObject.name + ": Missing EnemyBase, EnemyData, or EnemyAttackHandler. Disabling AI.");
            enabled = false;
            return;
        }

        if (primaryMeleeAttackData == null)
        {
            Debug.LogError(gameObject.name + ": PrimaryMeleeAttackData not assigned! Disabling AI.");
            enabled = false;
            return;
        }

        movementSpeed = enemyBase.enemyData.movementSpeed;
        engagementRangeForPrimaryAttack = primaryMeleeAttackData.optimalRangeMax > 0 ? primaryMeleeAttackData.optimalRangeMax : 1.5f; // Fallback
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerCombatController = playerObject.GetComponent<PlayerCombatController>();
        }
        else
        {
            Debug.LogError("Player not found! Disabling AI for " + gameObject.name);
            enabled = false;
            return;
        }

        currentState = EnemyState.Idle;
    }

    void Update()
    {
        if (playerTransform == null || !enabled || playerCombatController == null) return;

        if (currentState == EnemyState.Staggered)
        {
            HandleStaggeredState();
            return;
        }

        // FSM logic
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState();
                break;
            case EnemyState.Chase:
                HandleChaseState();
                break;
            case EnemyState.ExecutingAttackSequence:
                HandleExecutingAttackSequenceState();
                break;
            case EnemyState.Cooldown: 
                HandleCooldownState_FSM(); 
                break;
        }
    }

    void HandleIdleState()
    {
        enemyBase.spriteRenderer.color = enemyBase.originalSpriteColor; 

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chase;
        }
        if (movementController != null)
        {
            movementController.RequestAIStop(); 
        }
    }

    void HandleChaseState()
    {
        enemyBase.spriteRenderer.color = enemyBase.originalSpriteColor; 

        // Movement
        Vector3 directionToPlayer3D = playerTransform.position - transform.position;
        Vector2 directionToPlayer2D = new Vector2(directionToPlayer3D.x, directionToPlayer3D.y).normalized;
        if (movementController != null)
        {
            movementController.RequestAIMovement(directionToPlayer2D); 
        }
        else 
        {
            transform.position += new Vector3(directionToPlayer2D.x, directionToPlayer2D.y, 0) * movementSpeed * Time.deltaTime;
        }
        //transform.position += new Vector3(directionToPlayer2D.x, directionToPlayer2D.y, 0) * movementSpeed * Time.deltaTime;
        bool canAIRotate = true;
        if (movementController != null && movementController.IsMovementExternallyControlled)
        {

            if (movementController.IsOverrideActive) canAIRotate = false;
        }

        // Rotation
        if (canAIRotate && directionToPlayer2D != Vector2.zero)
        {
            float angle = Mathf.Atan2(directionToPlayer2D.y, directionToPlayer2D.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Check if ready to attack
        if (distanceToPlayer <= engagementRangeForPrimaryAttack &&
            !attackHandler.IsAttackOnCooldown(primaryMeleeAttackData) &&
            !attackHandler.isAttacking) // Ensure handler isn't busy from a previous call this frame
        {

            if (movementController != null)
            {
                movementController.RequestAIStop(); // Stop chasing before attacking
            }

            // Initiate attack through the handler
            if (attackHandler.StartAttackExecution(primaryMeleeAttackData))
            {
                currentState = EnemyState.ExecutingAttackSequence;
                //Debug.Log(gameObject.name + " FSM: Initiated attack via handler. Moving to ExecutingAttackSequence state.");
            }
            else
            {
                // Attack couldn't be started (e.g., another check failed in handler, though unlikely here)
                currentState = EnemyState.Cooldown;
                //Debug.LogWarning(gameObject.name + " FSM: AttackHandler failed to start attack. Remaining in Chase.");
            }
        }
        else if (distanceToPlayer > loseSightRange)
        {
            currentState = EnemyState.Idle;
        }
    }

    void HandleExecutingAttackSequenceState()
    {

        if (!attackHandler.isAttacking) // Attack sequence finished or was interrupted
        {

            currentState = EnemyState.Cooldown;
        }
    }


    void HandleCooldownState_FSM()
    {

        if (!attackHandler.IsAttackOnCooldown(primaryMeleeAttackData))
        {
        currentState = EnemyState.Chase;
        }
        //Debug.Log(gameObject.name + " FSM: Cooldown_FSM finished. Returning to Chase.");
    }


    void HandleStaggeredState()
    {
        enemyBase.spriteRenderer.color = Color.black; // Example visual
        if (movementController != null)
        {
            movementController.RequestAIStop(); // Ensure no movement during stagger
        }

        staggerTimer -= Time.deltaTime;
        if (staggerTimer <= 0)
        {
            currentState = EnemyState.Idle;
            enemyBase.SetStaggered(false);
        }
    }

    public void BecomeStaggered() // Called by EnemyAttackHandler
    {
        enemyBase.spriteRenderer.color = Color.black;
        if (currentState == EnemyState.Staggered) return;

        // If currently executing an attack, tell the handler to stop it
        if (attackHandler.isAttacking)
        {
            attackHandler.InterruptAttack(byParry: true); // Assuming stagger is due to parry
        }

        Debug.LogWarning(gameObject.name + " FSM: IS STAGGERED!");
        currentState = EnemyState.Staggered;
        staggerTimer = enemyBase.enemyData.staggerDuration;
        enemyBase.SetStaggered(true);
    }

    void OnDrawGizmosSelected()
    {
        if (enemyBase == null || playerTransform == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseSightRange);

        // Show the engagement range for its primary attack
        if (primaryMeleeAttackData != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, engagementRangeForPrimaryAttack);
        }


    }
}