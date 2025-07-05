using UnityEngine;
public enum WeaponType
{
    RayCast,
    Projectile,
    Melee
}
public enum AmmoType
{
    None, // For melee or weapons that don't use ammo
    Bullets,
    Shells,
    Rockets,
    EnergyCells
}

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName = "Weapon Name";
    public Sprite weaponIcon; 

    [Header("Weapon Type")]
    public WeaponType weaponType;

    [Header("Projectile Weapon Specifics (DO NOT USE IF NOT A PROJECTILE)")]
    public float projectileSpeed = 20f;
    public GameObject projectilePrefab; 
    public float projectileLifetime = 2f;
    public string projectileTargetTag;

    [Header("RayCast Weapon Specifics (DO NOT USE IF NOT A RAYCAST)")]
    public float maxRaycastDistance = 100f;

    [Header("Melee Weapon Specifics (DO NOT USE IF NOT A MELEE)")]
    public Vector3 hitboxSize = Vector3.one;
    public Vector3 hitboxOffset = Vector3.zero;
    public float attackRange = 1.5f;

    [Header("Shooting")]
    public float damage = 10f;
    public float fireRate = 0.5f; 

    [Header("Ammo")]
    public AmmoType ammoType = AmmoType.Bullets;
    public int ammoCostPerShot = 1; 



    [Header("Visuals & Prefabs")]
    public GameObject weaponPrefab;

    [Header("Effects (Optional - can be handled in specific weapon scripts too)")]
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;
}