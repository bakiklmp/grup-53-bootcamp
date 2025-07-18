using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyBase))]
public class EnemyMovementController : MonoBehaviour
{
    private Rigidbody2D rb;
    private EnemyBase enemyBase;
    private EnemyData enemyData;
    private AudioSource audioSource;
    private Transform ownerTransform;

    private Vector2 aiRequestedVelocityTarget = Vector2.zero;
    private bool aiMovementRequested = false;

    public bool IsOverrideActive { get; private set; }
    public bool IsMovementExternallyControlled => IsOverrideActive; 

    private MovementOverrideData currentOverrideData;
    private Transform overrideLungeTarget;

    private float overrideStartTime;
    private Vector2 overrideStartPosition;
    private Vector2 overrideTargetPosition;         
    private Quaternion overrideStartRotation;       
    private float initialDistanceToOverrideTarget;  

    private bool overrideObjectiveAchieved;
    private bool sfxVfxOnObjectiveReachedPlayed; 


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyBase = GetComponent<EnemyBase>();
        audioSource = GetComponent<AudioSource>();
        ownerTransform = transform;

        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            Debug.LogWarning($"{gameObject.name}: EnemyMovementController expects a Dynamic Rigidbody2D for velocity-based movement. Current type: {rb.bodyType}", this);
        }

        if (enemyBase != null && enemyBase.enemyData != null)
        {
            enemyData = enemyBase.enemyData;
        }
        else
        {
            Debug.LogError($"EnemyMovementController on {gameObject.name} could not find EnemyBase or EnemyData. Movement will not work correctly.", this);
            enabled = false;
            return;
        }
        //audioSource.playOnAwake = false;
    }

    void FixedUpdate()
    {
        if (!enabled || enemyData == null) return;

        if (IsOverrideActive)
        {
            ExecuteCurrentMovementOverride();
        }
        else if (aiMovementRequested)
        {
            ProcessAIMovement();
        }
        else
        {
        }
    }

    public void RequestAIMovement(Vector2 direction, float speedFactor = 1f)
    {
        if (IsOverrideActive) return;

        float targetSpeed = enemyData.movementSpeed * Mathf.Max(0, speedFactor);
        aiRequestedVelocityTarget = direction.normalized * targetSpeed;
        aiMovementRequested = true;
    }

    public void RequestAIStop()
    {
        aiMovementRequested = false;
        aiRequestedVelocityTarget = Vector2.zero;
        if (!IsOverrideActive) 
        {
            rb.linearVelocity = Vector2.zero; 
        }
    }

    private void ProcessAIMovement()
    {
        rb.linearVelocity = aiRequestedVelocityTarget;

    }

    public void StartMovementOverride(MovementOverrideData overrideData, Transform lungeTargetForOverride, Transform characterTransform)
    {
        if (overrideData.movementType == MovementOverrideType.None)
        {
            if (IsOverrideActive) StopMovementOverride(true);
            return;
        }

        if (IsOverrideActive) 
        {
            StopMovementOverride(false); 
        }

        IsOverrideActive = true;
        aiMovementRequested = false; 

        currentOverrideData = overrideData;
        overrideLungeTarget = lungeTargetForOverride;
        ownerTransform = characterTransform;

        overrideStartTime = Time.time;
        overrideStartPosition = rb.position;
        overrideStartRotation = ownerTransform.rotation;
        overrideObjectiveAchieved = false;
        sfxVfxOnObjectiveReachedPlayed = false;

        PlaySFX(currentOverrideData.sfxOnStart);
        SpawnVFX(currentOverrideData.vfxOnStart, ownerTransform.position, ownerTransform.rotation);

        switch (currentOverrideData.movementType)
        {
            case MovementOverrideType.LungeToTarget:
                if (overrideLungeTarget != null)
                {
                    Vector2 directionToTarget = ((Vector2)overrideLungeTarget.position - overrideStartPosition).normalized;
                    overrideTargetPosition = overrideStartPosition + directionToTarget * currentOverrideData.distance;
                }
                else
                {
                    Debug.LogWarning($"LungeToTarget on {gameObject.name} without a target. Will behave like HoldPosition.", this);
                    currentOverrideData.movementType = MovementOverrideType.HoldPosition; // Fallback
                    overrideTargetPosition = overrideStartPosition; // No movement
                }
                break;

            case MovementOverrideType.DashDirectional:
            case MovementOverrideType.StepDistance:
                Vector2 localDir = currentOverrideData.direction.normalized;
                Vector2 worldDir = ownerTransform.TransformDirection(localDir); // Convert local direction to world
                overrideTargetPosition = overrideStartPosition + worldDir * currentOverrideData.distance;
                break;

            case MovementOverrideType.HoldPosition:
                overrideTargetPosition = overrideStartPosition; // No movement target
                rb.linearVelocity = Vector2.zero; // Ensure it's stopped
                break;
        }
        initialDistanceToOverrideTarget = Vector2.Distance(overrideStartPosition, overrideTargetPosition);
        if (initialDistanceToOverrideTarget < 0.01f && currentOverrideData.movementType != MovementOverrideType.HoldPosition)
        {
            // Already at target or very close, or HoldPosition
            overrideObjectiveAchieved = true;
        }
    }

    public void StopMovementOverride(bool playEndEffects = true)
    {
        if (!IsOverrideActive) return;

        if (playEndEffects)
        {
            PlaySFX(currentOverrideData.sfxOnEnd);
            SpawnVFX(currentOverrideData.vfxOnEnd, ownerTransform.position, ownerTransform.rotation);
        }

        IsOverrideActive = false;
        rb.linearVelocity = Vector2.zero; // Ensure stop on override end
        currentOverrideData = null;
        overrideLungeTarget = null;
    }

    private void ExecuteCurrentMovementOverride()
    {
        float elapsedTime = Time.time - overrideStartTime;
        bool durationMet = currentOverrideData.movementDuration > 0 && elapsedTime >= currentOverrideData.movementDuration;

        HandleOverrideRotation();

        if (currentOverrideData.movementType == MovementOverrideType.HoldPosition)
        {
            rb.linearVelocity = Vector2.zero; // Enforce stillness
            if (durationMet) StopMovementOverride();
            return;
        }

        if (overrideObjectiveAchieved)
        {
            rb.linearVelocity = Vector2.zero; // Stay at achieved objective
            if (!sfxVfxOnObjectiveReachedPlayed)
            {
                PlaySFX(currentOverrideData.sfxOnObjectiveReached);
                SpawnVFX(currentOverrideData.vfxOnObjectiveReached, ownerTransform.position, ownerTransform.rotation);
                sfxVfxOnObjectiveReachedPlayed = true;
            }
            if (durationMet || currentOverrideData.movementDuration == 0) // If duration also met OR no specific duration (move till completion)
            {
                StopMovementOverride();
            }
            return;
        }

        if (durationMet) // Duration met before reaching target
        {
            StopMovementOverride();
            return;
        }

        // Calculate movement towards overrideTargetPosition
        Vector2 currentPosition = rb.position;
        float distanceToFinalTarget = Vector2.Distance(currentPosition, overrideTargetPosition);

        if (distanceToFinalTarget < 0.05f) // Threshold to consider objective reached
        {
            overrideObjectiveAchieved = true;
            rb.position = overrideTargetPosition; // Snap to exact final position
            rb.linearVelocity = Vector2.zero;
            // Objective reached effects will be played in the next FixedUpdate tick due to the flag
            return;
        }

        Vector2 directionToTargetPoint = (overrideTargetPosition - currentPosition).normalized;
        float baseSpeed = enemyData.movementSpeed * currentOverrideData.speedFactor;
        float easedSpeed = baseSpeed;

        // Apply Easing
        if (currentOverrideData.easingFunction != EasingFunctionType.None)
        {
            float progress = 0f;
            if (currentOverrideData.movementDuration > 0) // Time-based progress
            {
                progress = Mathf.Clamp01(elapsedTime / currentOverrideData.movementDuration);
            }
            else if (initialDistanceToOverrideTarget > 0.01f) // Distance-based progress (if no duration)
            {
                progress = Mathf.Clamp01(1f - (distanceToFinalTarget / initialDistanceToOverrideTarget));
            }
            else { progress = 1f; } // Should have been caught by objective achieved already


            float easedProgressFactor = EasingFunctions.ApplyEasing(progress, currentOverrideData.easingFunction);

            if (currentOverrideData.easingFunction == EasingFunctionType.EaseOutQuad)
            {
                easedSpeed = baseSpeed * (1f - progress + 0.1f); // Crude approximation, ensure some speed
                easedSpeed = baseSpeed * (2f * (1f - progress)); // Based on derivative
            }
            else if (currentOverrideData.easingFunction == EasingFunctionType.EaseInOutQuad)
            {
                // Derivative of EaseInOutQuad is more complex (4t for t<0.5, 4(1-t) for t>=0.5)
                // For simplicity, let's use a curve that peaks at mid-progress
                float speedMod = (progress < 0.5f) ? (4f * progress) : (4f * (1f - progress));
                easedSpeed = baseSpeed * speedMod;
            }
            easedSpeed = Mathf.Max(easedSpeed, baseSpeed * 0.1f); // Ensure a minimum speed to prevent stalling if easing logic is imperfect
        }


        rb.linearVelocity = directionToTargetPoint * Mathf.Max(0, easedSpeed); // Ensure non-negative speed
    }

    private void HandleOverrideRotation()
    {
        if (currentOverrideData == null) return;

        switch (currentOverrideData.rotationType)
        {
            case OverrideRotationType.MaintainAIRotation:
                // Do nothing, AI or other systems handle rotation
                break;
            case OverrideRotationType.LockRotationAtStart:
                ownerTransform.rotation = overrideStartRotation;
                break;
            case OverrideRotationType.FaceMovementDirection:
                if (rb.linearVelocity.sqrMagnitude > 0.01f) // Only rotate if moving
                {
                    float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f; // -90 for sprite up
                    ownerTransform.rotation = Quaternion.Slerp(ownerTransform.rotation, Quaternion.Euler(0, 0, angle), Time.fixedDeltaTime * 10f); // Smooth rotation
                }
                break;
            case OverrideRotationType.FaceLungeTarget:
                if (overrideLungeTarget != null)
                {
                    Vector2 directionToLungeTarget = (overrideLungeTarget.position - ownerTransform.position).normalized;
                    if (directionToLungeTarget.sqrMagnitude > 0.01f)
                    {
                        float angle = Mathf.Atan2(directionToLungeTarget.y, directionToLungeTarget.x) * Mathf.Rad2Deg - 90f;
                        ownerTransform.rotation = Quaternion.Slerp(ownerTransform.rotation, Quaternion.Euler(0, 0, angle), Time.fixedDeltaTime * 10f);
                    }
                }
                break;
            case OverrideRotationType.Spin:
                ownerTransform.Rotate(0, 0, currentOverrideData.spinSpeed * Time.fixedDeltaTime);
                break;
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void SpawnVFX(GameObject vfxPrefab, Vector3 position, Quaternion rotation)
    {
        if (vfxPrefab != null)
        {

            GameObject vfxInstance = Instantiate(vfxPrefab, position, rotation);

        }
    }
}