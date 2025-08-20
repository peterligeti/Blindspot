using UnityEngine;

public enum SpreadMode
{
    Random,
    Cone
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon")]
public class Weapon : ScriptableObject
{
    public string weaponName = "Default Gun";
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float fireRate = 0.2f; // Shots per second
    public int damage = 1;

    [Header("Spread & Multi-shot")]
    public float spread = 0f; // degrees
    public int bulletsPerShot = 1;
    public SpreadMode spreadMode = SpreadMode.Random; // NEW toggle

    public AudioClip shootSfx;
}