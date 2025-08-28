using UnityEngine;

public class EnemyLootPickupLogic : MonoBehaviour
{
    public enum LootType { Health, Weapon }

    [Header("General Settings")]
    [SerializeField] private LootType lootType = LootType.Health;

    [Header("Health Settings")]
    [SerializeField] private int healthAmount = 1;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip healthPackClip;

    [Header("Weapon Settings")]
    [SerializeField] private Weapon weaponToGive;
    [SerializeField] private AudioClip weaponPickupClip;
    [SerializeField] private GameObject pickupPromptUI; // "Press E"
    [SerializeField] private float pickupRange = 2f;

    private Transform player;
    private Shooter playerShooter;
    private bool isInRange = false;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player != null)
            playerShooter = player.GetComponent<Shooter>();

        if (pickupPromptUI != null)
            pickupPromptUI.SetActive(false);
    }

    private void Update()
    {
        if (lootType != LootType.Weapon || player == null) return;

        float distance = Vector2.Distance(player.position, transform.position);

        if (distance <= pickupRange)
        {
            if (pickupPromptUI != null)
                pickupPromptUI.SetActive(true);

            isInRange = true;

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (playerShooter != null && weaponToGive != null)
                {
                    playerShooter.SetWeapon(weaponToGive);

                    if (sfxSource != null && weaponPickupClip != null)
                        sfxSource.PlayOneShot(weaponPickupClip);

                    Destroy(gameObject); // remove pickup
                }
            }
        }
        else
        {
            if (pickupPromptUI != null)
                pickupPromptUI.SetActive(false);
            isInRange = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (lootType != LootType.Health) return;

        if (other.CompareTag("Player"))
        {
            if (sfxSource != null && healthPackClip != null)
                sfxSource.PlayOneShot(healthPackClip);

            PlayerHealthLogic playerHealth = other.GetComponent<PlayerHealthLogic>();
            if (playerHealth != null)
            {
                playerHealth.AddHealthPoints(healthAmount);
                Destroy(gameObject);
            }
        }
    }
}
