using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    [Header("Melee Specifics")]
    [Tooltip("Layers that this melee weapon can hit.")]
    public LayerMask targetLayers;

    // To prevent hitting the same enemy multiple times in one swing
    private HashSet<Collider2D> _hitTargetsThisSwing;

    public override void Initialize(PlayerWeaponManager manager, PlayerInputHandler inputHandler, WeaponData data)
    {
        base.Initialize(manager, inputHandler, data);
        if (data.weaponType != WeaponType.Melee)
        {
            Debug.LogError($"MeleeWeapon script assigned to weapon data '{data.weaponName}' that is not of type Melee!", this);
        }
        _hitTargetsThisSwing = new HashSet<Collider2D>();
    }

    protected override void PerformAttack(Vector2 attackDirection)
    {
        if (ownerWeaponManager == null)
        {
            Debug.LogError("OwnerWeaponManager is null in MeleeWeapon.", this);
            return;
        }
        if (weaponData == null)
        {
            Debug.LogError("WeaponData is null in MeleeWeapon.", this);
            return;
        }

        _hitTargetsThisSwing.Clear(); // Clear previously hit targets for this new swing

        Transform attackerTransform = ownerWeaponManager.transform;

        float attackAngleRad = Mathf.Atan2(attackDirection.y, attackDirection.x);
        float attackAngleDeg = attackAngleRad * Mathf.Rad2Deg;

        Vector2 localOffset = new Vector2(weaponData.hitboxOffset.x, weaponData.hitboxOffset.y);
        Vector2 rotatedOffset = new Vector2(
            localOffset.x * Mathf.Cos(attackAngleRad) - localOffset.y * Mathf.Sin(attackAngleRad),
            localOffset.x * Mathf.Sin(attackAngleRad) + localOffset.y * Mathf.Cos(attackAngleRad)
        );

        Vector2 hitboxCenter = (Vector2)attackerTransform.position + rotatedOffset;

        Vector2 hitboxSize = new Vector2(weaponData.hitboxSize.x, weaponData.hitboxSize.y);

        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(hitboxCenter, hitboxSize, attackAngleDeg, targetLayers);

        Debug.Log($"[MeleeWeapon] {weaponData.weaponName} attacking. Hits: {hitColliders.Length}. Center: {hitboxCenter}, Size: {hitboxSize}, Angle: {attackAngleDeg}");

        foreach (Collider2D hitCollider in hitColliders)
        {
            if (hitCollider.transform == attackerTransform || hitCollider.transform.IsChildOf(attackerTransform))
            {
                continue;
            }

            if (_hitTargetsThisSwing.Contains(hitCollider))
            {
                continue; // Skip the rest of the code and go to the next collider for cheking
            }

            EnemyBase enemy = hitCollider.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                _hitTargetsThisSwing.Add(hitCollider); // Add to set to prevent re-hitting
                enemy.TakeDamage(weaponData.damage);

                Debug.Log($"[MeleeWeapon] Hit {hitCollider.name} with {weaponData.weaponName} for {weaponData.damage} damage.");

                if (weaponData.hitEffectPrefab != null)
                {
                    Instantiate(weaponData.hitEffectPrefab, hitCollider.bounds.center, Quaternion.LookRotation(Vector3.forward, attackDirection));
                }
            }
        }

    }

    protected override void PlayMuzzleFlash()
    {
 
    }


    protected virtual void OnDrawGizmosSelected()
    {
        if (weaponData == null || weaponData.weaponType != WeaponType.Melee)
            return;

        Transform gizmoOriginTransform = transform; // Default to weapon's own transform
        Vector2 attackDirectionForGizmo = transform.right; // Default direction

        if (Application.isPlaying && ownerWeaponManager != null && playerInputHandler != null)
        {
            gizmoOriginTransform = ownerWeaponManager.transform;
            attackDirectionForGizmo = playerInputHandler.CalculatedWorldAimDirection.normalized;
            if (attackDirectionForGizmo == Vector2.zero)
            {
                attackDirectionForGizmo = gizmoOriginTransform.right; // Or whatever your player's default facing is
            }
        }
        else if (ownerWeaponManager != null) // If owner exists but not full input (e.g. preview)
        {
            gizmoOriginTransform = ownerWeaponManager.transform;
            attackDirectionForGizmo = gizmoOriginTransform.right;
        }


        float attackAngleRad = Mathf.Atan2(attackDirectionForGizmo.y, attackDirectionForGizmo.x);
        float attackAngleDeg = attackAngleRad * Mathf.Rad2Deg;

        Vector2 localOffset = new Vector2(weaponData.hitboxOffset.x, weaponData.hitboxOffset.y);
        Vector2 rotatedOffset = new Vector2(
            localOffset.x * Mathf.Cos(attackAngleRad) - localOffset.y * Mathf.Sin(attackAngleRad),
            localOffset.x * Mathf.Sin(attackAngleRad) + localOffset.y * Mathf.Cos(attackAngleRad)
        );

        Vector2 hitboxCenter = (Vector2)gizmoOriginTransform.position + rotatedOffset;
        Vector2 hitboxSizeGizmo = new Vector2(weaponData.hitboxSize.x, weaponData.hitboxSize.y);

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red, semi-transparent

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(hitboxCenter, Quaternion.Euler(0, 0, attackAngleDeg), new Vector3(hitboxSizeGizmo.x, hitboxSizeGizmo.y, 1f));
        Gizmos.DrawCube(Vector3.zero, Vector3.one); // Draw a unit cube, scaled and rotated by the matrix
        Gizmos.matrix = oldMatrix;

    }
}