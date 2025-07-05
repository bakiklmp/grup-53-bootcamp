using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("Weapon Setup")]
    [Tooltip("Transform point where weapons will be parented and positioned.")]
    public Transform weaponHoldPoint;
    [Tooltip("Initial weapon data to grant the player at start. Can be empty.")]
    public List<WeaponData> startingWeaponsData;
    [SerializeField] private int startingAmmo = 50;



    [Header("Runtime State (Visible for debugging)")]
    [SerializeField] private List<Weapon> possessedWeapons = new List<Weapon>();
    [SerializeField] private Weapon currentWeapon;
    [SerializeField] private int currentWeaponIndex = -1;

    public PlayerInputHandler playerInputHandler;

    // Ammo management
    // Using a Dictionary to store ammo counts for each AmmoType
    private Dictionary<AmmoType, int> ammoInventory = new Dictionary<AmmoType, int>();

    void Start()
    {
        InitializeAmmoInventory();
        GrantStartingWeapons();

        // Equip the first weapon if any
        if (possessedWeapons.Count > 0)
        {
            EquipWeapon(0);
        }
    }

    void InitializeAmmoInventory()
    {
        // Initialize all ammo types with ammo, later add max ammo for different types of ammo
        foreach (AmmoType ammoT in System.Enum.GetValues(typeof(AmmoType)))
        {
            if (ammoT != AmmoType.None)
            {
                ammoInventory[ammoT] = startingAmmo;
            }
        }
        Debug.Log("Ammo inventory initialized.");
    }

    void GrantStartingWeapons()
    {
        if (weaponHoldPoint == null)
        {
            Debug.LogError("WeaponHoldPoint is not assigned on PlayerWeaponManager!");
            return;
        }

        foreach (WeaponData data in startingWeaponsData)
        {
            AddWeapon(data);
        }
    }

    public bool AddWeapon(WeaponData weaponDataToAdd)
    {
        if (weaponDataToAdd == null)
        {
            Debug.LogWarning("Tried to add a null WeaponData.");
            return false;
        }


        if (weaponDataToAdd.weaponPrefab == null)
        {
            Debug.LogError($"WeaponData '{weaponDataToAdd.weaponName}' has no weaponPrefab assigned!");
            return false;
        }

        if (playerInputHandler == null)
        {
            Debug.LogError($"InputHandler is not assigned! in {this}");
            return false;
        }


        GameObject weaponObject = Instantiate(weaponDataToAdd.weaponPrefab, weaponHoldPoint);
        Weapon newWeaponInstance = weaponObject.GetComponent<Weapon>();

        if (newWeaponInstance == null)
        {
            Debug.LogError($"The prefab for '{weaponDataToAdd.weaponName}' does not have a Weapon-derived script attached!");
            Destroy(weaponObject);
            return false;
        }

        newWeaponInstance.Initialize(this, playerInputHandler, weaponDataToAdd); 
        possessedWeapons.Add(newWeaponInstance);
        weaponObject.SetActive(false); 

        Debug.Log($"Added {weaponDataToAdd.weaponName} to inventory.");

        if (currentWeapon == null && possessedWeapons.Count == 1)
        {
            EquipWeapon(0);
        }

        return true;
    }

    public void EquipWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= possessedWeapons.Count)
        {
            Debug.LogWarning($"Invalid weapon index: {weaponIndex}");
            return;
        }

        // Unequip current weapon if there is one
        if (currentWeapon != null)
        {
            currentWeapon.Unequip();
        }

        // Equip new weapon
        currentWeaponIndex = weaponIndex;
        currentWeapon = possessedWeapons[currentWeaponIndex];
        currentWeapon.Equip();

        Debug.Log($"Equipped {currentWeapon.weaponData.weaponName}.");
    }

    public void CycleNextWeapon()
    {
        if (possessedWeapons.Count <= 1) return; // Not enough weapons to cycle

        int nextIndex = (currentWeaponIndex + 1) % possessedWeapons.Count;
        EquipWeapon(nextIndex);
    }

    public void CyclePreviousWeapon()
    {
        if (possessedWeapons.Count <= 1) return;

        int prevIndex = (currentWeaponIndex - 1 + possessedWeapons.Count) % possessedWeapons.Count;
        EquipWeapon(prevIndex);
    }

    public void AttemptFireCurrentWeapon()
    {
        if (currentWeapon == null)
        {
            Debug.Log("No weapon equipped to fire.");
            return;
        }

        bool success = currentWeapon.TryAttack(); 
        if (success)
        {
        }
    }

    public void AddAmmo(AmmoType type, int amount)
    {
        if (type == AmmoType.None) return; 

        if (!ammoInventory.ContainsKey(type))
        {
            Debug.LogWarning($"Trying to add ammo for uninitialized type: {type}. Initializing now.");
            ammoInventory[type] = 0;
        }

        ammoInventory[type] += amount;
        Debug.Log($"Added {amount} of {type}. Total: {ammoInventory[type]}");
    }

    public bool HasEnoughAmmo(AmmoType type, int amountNeeded)
    {
        if (type == AmmoType.None) return true;

        return ammoInventory.ContainsKey(type) && ammoInventory[type] >= amountNeeded;
    }

    public void SpendAmmo(AmmoType type, int amountToSpend)
    {
        if (type == AmmoType.None) return; 

        if (HasEnoughAmmo(type, amountToSpend))
        {
            ammoInventory[type] -= amountToSpend;
            Debug.Log($"Spent {amountToSpend} of {type}. Remaining: {ammoInventory[type]}");
        }
        else
        {
            Debug.LogWarning($"Tried to spend {amountToSpend} of {type}, but not enough ammo.");
        }
    }

    public int GetCurrentAmmo(AmmoType type)
    {
        if (type == AmmoType.None) return 999; 
        if (ammoInventory.TryGetValue(type, out int count))
        {
            return count;
        }
        return 0;
    }

    void OnDrawGizmosSelected()
    {
        if (weaponHoldPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(weaponHoldPoint.position, 0.1f);
            Gizmos.DrawLine(weaponHoldPoint.position, weaponHoldPoint.position + weaponHoldPoint.forward * 0.5f);
        }
    }
}