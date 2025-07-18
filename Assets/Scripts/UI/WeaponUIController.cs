using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI weaponNameText;

    [SerializeField] private PlayerWeaponManager weaponManager;
    [SerializeField] private Weapon currentWeapon;

    void Start()
    {


        if (weaponManager != null)
        {
            weaponManager.OnWeaponSwitched += HandleWeaponSwitch;

            weaponManager.OnAmmoInventoryChanged += HandleAmmoInventoryChange;

            if (weaponManager.currentWeapon != null)
            {
                HandleWeaponSwitch(weaponManager.currentWeapon);
            }
        }
    }

    private void OnDestroy()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponSwitched -= HandleWeaponSwitch;
            weaponManager.OnAmmoInventoryChanged -= HandleAmmoInventoryChange;
        }
    }

    private void HandleWeaponSwitch(Weapon newWeapon)
    {
        currentWeapon = newWeapon;

        if (currentWeapon != null && currentWeapon.weaponData != null)
        {
            weaponIconImage.sprite = currentWeapon.weaponData.weaponIcon;

            if (weaponNameText != null)
            {
                weaponNameText.text = currentWeapon.weaponData.weaponName;
            }

            UpdateAmmoDisplay();
        }
    }
    private void HandleAmmoInventoryChange(AmmoType type, int newAmount)
    {
        if (currentWeapon != null && currentWeapon.weaponData.ammoType == type)
        {
            UpdateAmmoDisplay();
        }
    }

    private void UpdateAmmoDisplay()
    {
        if (currentWeapon == null || weaponManager == null) return;

        WeaponData data = currentWeapon.weaponData;

        if (data.ammoType == AmmoType.None)
        {
            ammoText.text = "∞"; 
        }
        else
        {
            int currentAmmoCount = weaponManager.GetCurrentAmmo(data.ammoType);
            ammoText.text = currentAmmoCount.ToString();
        }
    }
}