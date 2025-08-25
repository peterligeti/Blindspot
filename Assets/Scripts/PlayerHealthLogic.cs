using System;
using UnityEngine;
using TMPro;

public class PlayerHealthLogic : MonoBehaviour
{
    [SerializeField] private int startingMaxHealth = 3;
    private static int persistentMaxHealth = -1; // keeps value across respawns

    private int currentHealth;
    
    public static event Action OnPlayerDeath;

    private TextMeshProUGUI playerHealthText;
    private TextMeshProUGUI playerMaxHealthText;

    private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashDuration = 0.1f;
    private Color originalColor;

    void Start()
    {
        if (persistentMaxHealth == -1)
            persistentMaxHealth = startingMaxHealth;

        currentHealth = persistentMaxHealth;

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        
        // Setup UI
        GameObject textObject = GameObject.Find("PlayerHealthPoint");
        if (textObject != null)
        {
            playerHealthText = textObject.GetComponent<TextMeshProUGUI>();
            playerHealthText.text = $"Health {currentHealth}";
        }
        
        GameObject textMaxObject = GameObject.Find("PlayerMaxHealthPoint");
        if (textMaxObject != null)
        {
            playerMaxHealthText = textMaxObject.GetComponent<TextMeshProUGUI>();
            playerMaxHealthText.text = $"Max Health {persistentMaxHealth}";
        }
    }

    public void TakeDamage(int damage)
    {
        FlashWhenHurt();
        
        currentHealth -= damage;
        if (playerHealthText != null)
            playerHealthText.text = $"Health {currentHealth}";

        if (currentHealth <= 0)
            Die();
    }

    public void AddHealthPoints(int amount)
    {
        currentHealth += amount;
        if (playerHealthText != null)
            playerHealthText.text = $"Health {currentHealth}";

        if (currentHealth > persistentMaxHealth)
        {
            persistentMaxHealth += amount;
            if (playerMaxHealthText != null)
                playerMaxHealthText.text = $"Max Health {persistentMaxHealth}";
            Debug.Log("Max Health Increased to: " + persistentMaxHealth);
        }
    }

    private void FlashWhenHurt() => StartCoroutine(Flash());

    private System.Collections.IEnumerator Flash()
    {
        spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        OnPlayerDeath?.Invoke();
        currentHealth = persistentMaxHealth;
        StartCoroutine(DieAfterFlash());
        if (playerHealthText != null)
            playerHealthText.text = $"Health {currentHealth}";
    }

    private System.Collections.IEnumerator DieAfterFlash()
    {
        spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
        yield return new WaitForSeconds(flashDuration);
        Destroy(gameObject);
    }
}
