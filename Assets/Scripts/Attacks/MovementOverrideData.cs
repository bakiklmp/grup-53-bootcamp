using UnityEngine;

[System.Serializable]
public class MovementOverrideData
{
    public MovementOverrideType movementType = MovementOverrideType.None;

    [Header("General Movement Params")]
    [Tooltip("Speed multiplier relative to enemy's base speed. 1 = base speed.")]
    public float speedFactor = 1.0f;
    [Tooltip("Distance for lunges, dashes, steps.")]
    public float distance = 3f;
    [Tooltip("Direction for DashDirectional (local space). Not used by LungeToTarget or HoldPosition.")]
    public Vector2 direction = Vector2.zero; // Renamed from directionOrPathTarget for clarity
    [Tooltip("Duration of this movement override. If 0, might last until target reached or phase ends.")]
    public float movementDuration = 0.5f;

    [Header("Rotation During Override")]
    public OverrideRotationType rotationType = OverrideRotationType.MaintainAIRotation;
    [Tooltip("Speed of rotation in degrees per second if rotationType is Spin.")]
    public float spinSpeed = 360f;

    [Header("Easing for Override Movement")]
    [Tooltip("Easing function to apply to the movement speed profile.")]
    public EasingFunctionType easingFunction = EasingFunctionType.None;

    [Header("SFX & VFX Triggers")]
    public AudioClip sfxOnStart;
    public GameObject vfxOnStart; // Prefab
    public AudioClip sfxOnObjectiveReached;
    public GameObject vfxOnObjectiveReached; // Prefab
    public AudioClip sfxOnEnd;
    public GameObject vfxOnEnd; // Prefab

}