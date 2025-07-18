public enum AttackActionType
{
    Melee,
    Projectile,
    SpawnEnemy, 
    EffectOnly, 
    Wait,       
    CustomScriptedEvent
}

public enum ProjectilePatternType
{
    Single,
    Spread,         // Multiple projectiles in an arc
    Volley,         // Multiple projectiles in sequence from same point
    CircleBurst,    // Projectiles in all directions
    Spiral,
    // Add more as needed: Homing, Beam, Wall, etc.
}

public enum MovementOverrideType
{
    None,
    LungeToTarget,
    DashDirectional,
    StepDistance,
    HoldPosition,
}

public enum OverrideRotationType
{
    MaintainAIRotation,     // AI or other systems control rotation
    LockRotationAtStart,    // Keep the rotation from when the override began
    FaceMovementDirection,  // Turn to face the direction of current velocity
    FaceLungeTarget,        // Continuously face the lunge target (if applicable)
    Spin                    // Rotate continuously
}

public enum EasingFunctionType
{
    None,                   // Linear movement
    EaseOutQuad,            // Decelerates towards the end
    EaseInOutQuad,          // Accelerates at start, decelerates at end
    // Add more like EaseInQuad if needed
}

public enum HitboxShape
{
    Circle,
    Box,
    Arc // For wide swings
}