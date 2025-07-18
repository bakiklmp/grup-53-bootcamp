using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("References (Assigned at runtime)")]
    public WeaponData weaponData; // The ScriptableObject defining this weapon's stats

    protected PlayerInputHandler playerInputHandler;
    protected PlayerWeaponManager ownerWeaponManager; // The manager that owns/controls this weapon
    protected float lastAttackTime = -Mathf.Infinity; // Initialize to allow immediate first shot

    // Called by PlayerWeaponManager when this weapon instance is created and given to the player.
    public virtual void Initialize(PlayerWeaponManager manager, PlayerInputHandler inputHandler ,  WeaponData data)
    {
        ownerWeaponManager = manager;
        playerInputHandler = inputHandler;
        weaponData = data;

        // Ensure the weapon is ready or reset its state if needed
        gameObject.SetActive(false); // Start inactive until equipped
    }


    // Called when the weapon is equipped by the player.
    // Handles showing the weapon, playing equip animations/sounds, etc.
    public virtual void Equip()
    {
        gameObject.SetActive(true);
        lastAttackTime = -Mathf.Infinity;
        Debug.Log($"{weaponData.weaponName} equipped.");
    }
    public virtual void Unequip()
    {
        gameObject.SetActive(false);
        Debug.Log($"{weaponData.weaponName} unequipped.");
    }
    public virtual bool TryAttack()
    {
        if (playerInputHandler == null)
        {
            Debug.LogError("PlayerInputHandler not available in Weapon for aiming.", this);
            return false;
        }

        // Check fire rate
        if (Time.time < lastAttackTime + weaponData.fireRate)
        {
            //Debug.Log("Fire rate cooldown.");
            return false; // Still on cooldown
        }

        // Check ammo with the PlayerWeaponManager
        if (ownerWeaponManager != null && // Ensure owner is set
            weaponData.ammoType != AmmoType.None && // Only check if it uses ammo
            !ownerWeaponManager.HasEnoughAmmo(weaponData.ammoType, weaponData.ammoCostPerShot))
        {
            Debug.Log($"Not enough {weaponData.ammoType} ammo for {weaponData.weaponName}.");
            return false;
        }

        Vector2 fireDirection = playerInputHandler.CalculatedWorldAimDirection;

        if (fireDirection == Vector2.zero)
        {
            Debug.LogWarning("Attempting to fire with zero direction.");
            // fireDirection = GetMuzzleTransform() != null ? GetMuzzleTransform().right : transform.right; // Example fallback
        }

        PerformAttack(fireDirection); // This is the abstract method implemented by subclasses
        lastAttackTime = Time.time;

        if (ownerWeaponManager != null && weaponData.ammoType != AmmoType.None && weaponData.ammoCostPerShot > 0)
        { 
            ownerWeaponManager.SpendAmmo(weaponData.ammoType, weaponData.ammoCostPerShot);
            Debug.Log($"{weaponData.weaponName} ammo {weaponData.ammoType} decreased");
        }

        PlayMuzzleFlash();

        return true;
    }

    protected abstract void PerformAttack(Vector2 firePosition);


    protected virtual void PlayMuzzleFlash()
    {
        if (weaponData.muzzleFlashPrefab != null)
        {
            // Debug.Log("Muzzle flash for " + weaponData.weaponName);
        }
    }
}