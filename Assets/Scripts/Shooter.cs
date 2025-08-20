using UnityEngine;

public class Shooter : MonoBehaviour
{
    [SerializeField] private Weapon currentWeapon;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform spawnedBulletsContainer;
    [SerializeField] private AudioSource sfxSource;

    private float lastShotTime = -Mathf.Infinity;
    public string shooterTag = "Player";

    public void Shoot(Vector3 direction)
    {
        if (currentWeapon == null) return;
        if (Time.time < lastShotTime + 1f / currentWeapon.fireRate) return;

        lastShotTime = Time.time;

        int bulletCount = currentWeapon.bulletsPerShot;

        switch (currentWeapon.spreadMode)
        {
            case SpreadMode.Random:
                // 🔫 Random spread
                for (int i = 0; i < bulletCount; i++)
                {
                    float randomAngle = Random.Range(-currentWeapon.spread, currentWeapon.spread);
                    Vector3 spreadDirection = Quaternion.Euler(0, 0, randomAngle) * direction;
                    FireBullet(spreadDirection);
                }
                break;

            case SpreadMode.Cone:
                // 🔫 Even cone spread
                if (bulletCount == 1)
                {
                    FireBullet(direction);
                }
                else
                {
                    float totalSpread = currentWeapon.spread;
                    float angleStep = bulletCount > 1 ? totalSpread / (bulletCount - 1) : 0f;
                    float startAngle = -totalSpread / 2f;

                    for (int i = 0; i < bulletCount; i++)
                    {
                        float angleOffset = startAngle + angleStep * i;
                        Vector3 spreadDirection = Quaternion.Euler(0, 0, angleOffset) * direction;
                        FireBullet(spreadDirection);
                    }
                }
                break;
        }

        // Play sound once
        if (sfxSource != null && currentWeapon.shootSfx != null)
            sfxSource.PlayOneShot(currentWeapon.shootSfx);
    }

    private void FireBullet(Vector3 dir)
    {
        GameObject bullet = Instantiate(
            currentWeapon.bulletPrefab,
            firePoint.position,
            Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg),
            spawnedBulletsContainer
        );

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = dir.normalized * currentWeapon.bulletSpeed;

        BulletLogic bulletScript = bullet.GetComponent<BulletLogic>();
        if (bulletScript != null)
        {
            bulletScript.shooterTag = shooterTag;
            bulletScript.direction = dir.normalized;
            bulletScript.damage = currentWeapon.damage;
        }
    }

    public void SetWeapon(Weapon newWeapon)
    {
        currentWeapon = newWeapon;
    }
}
