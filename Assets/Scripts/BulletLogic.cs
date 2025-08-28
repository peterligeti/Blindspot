using UnityEngine;

public class BulletLogic : MonoBehaviour
{
    public Vector2 direction;
    [SerializeField] float lifetime = 1f;
    public string shooterTag; // "Player" or "Enemy"
    public int damage; // <-- now set from the weapon
    public bool destroyWhenHitSomeone = true;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(shooterTag)) return;

        if (shooterTag == "Player" && other.CompareTag("Enemy"))
        {
            EnemyHealthLogic enemyHealth = other.GetComponent<EnemyHealthLogic>();
            if (enemyHealth != null) enemyHealth.TakeDamage(damage);
            
            if (destroyWhenHitSomeone) Destroy(gameObject);
        }

        if (shooterTag == "Enemy" && other.CompareTag("Player"))
        {
            PlayerHealthLogic playerHealth = other.GetComponent<PlayerHealthLogic>();
            if (playerHealth != null) playerHealth.TakeDamage(damage);
            
            if (destroyWhenHitSomeone) Destroy(gameObject);
        }
    }
}