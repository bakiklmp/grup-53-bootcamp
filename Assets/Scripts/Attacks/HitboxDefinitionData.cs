using UnityEngine;

[System.Serializable]
public class HitboxDefinitionData
{
    public HitboxShape shape = HitboxShape.Circle;

    [Tooltip("Offset from the enemy's attack pivot point (e.g., transform.position or a child 'AttackOrigin' transform).")]
    public Vector2 offset = Vector2.up; // Example: 1 unit in front if enemy faces up

    [Tooltip("Radius if Circle or Arc, Width if Box.")]
    public float primarySize = 0.5f; // e.g., radius

    [Tooltip("Height if Box, Angle if Arc (degrees).")]
    public float secondarySize = 0.5f; // e.g., box height or arc angle

    [Tooltip("When within the phase's duration the hitbox becomes active (e.g., 0.1s into a 0.5s phase). Can also be driven by animation events.")]
    // When, within this phase's duration, the hitbox actually turns ON.
    // Before this, the enemy might be winding up.
    public float hitActiveStartTime = 0.1f;

    [Tooltip("How long the hitbox stays active within the phase.")]
    // Once the hitbox is ON, how long it stays ON.
    // This is the window where damage can be dealt.
    public float hitActiveDuration = 0.2f;
}