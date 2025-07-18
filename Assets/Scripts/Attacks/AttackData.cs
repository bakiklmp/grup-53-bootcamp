using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAttackData", menuName = "Combat/Attack Data")]
public class AttackData : ScriptableObject
{
    [Tooltip("Name of the attack for debugging and editor identification.")]
    public string attackName = "Unnamed Attack";

    [Tooltip("Optional icon for visual AI editors or debugging tools.")]
    public Sprite icon;

    [Tooltip("The sequence of phases that make up this entire attack.")]
    public List<AttackPhaseData> phases = new List<AttackPhaseData>();

    [Header("Attack Properties")]
    [Tooltip("Cooldown in seconds after the entire attack sequence completes before it can be used again.")]
    public float cooldownTime = 2.0f;

    [Tooltip("Helps AI decide if this attack is suitable based on range to target.")]
    public float optimalRangeMin = 0f;
    public float optimalRangeMax = 5f;

    public bool requiresLineOfSight = true;

}