using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("References (Assigned at runtime)")]
    public WeaponData weaponData; 

    protected PlayerInputHandler playerInputHandler;
    protected PlayerWeaponManager ownerWeaponManager; 
    protected float lastAttackTime = -Mathf.Infinity; 

    public virtual void Initialize(PlayerWeaponManager manager, PlayerInputHandler inputHandler, WeaponData data)
    {
        ownerWeaponManager = manager;
        playerInputHandler = inputHandler;
        weaponData = data;

        gameObject.SetActive(false); 
    }


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

        if (Time.time < lastAttackTime + weaponData.fireRate)
        {
            return false;
        }

        if (ownerWeaponManager != null && 
            weaponData.ammoType != AmmoType.None && 
            !ownerWeaponManager.HasEnoughAmmo(weaponData.ammoType, weaponData.ammoCostPerShot))
        {
            Debug.Log($"Not enough {weaponData.ammoType} ammo for {weaponData.weaponName}.");
            return false;
        }

        Vector2 fireDirection = playerInputHandler.CalculatedWorldAimDirection;

        if (fireDirection == Vector2.zero)
        {
            Debug.LogWarning("Attempting to fire with zero direction.");
        }

        PerformAttack(fireDirection); 
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

        }
    }
}