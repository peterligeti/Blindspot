using System;
using UnityEngine;
using TMPro;

public class PlayerHealthLogic : MonoBehaviour
{
    [SerializeField] int maxHealth = 3;
    private int currentHealth;
    
    public static event Action OnPlayerDeath;

    private TextMeshProUGUI playerHealthText;
    
    private SpriteRenderer spriteRenderer;
    [SerializeField] float flashDuration = 0.1f;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        
        GameObject textObject = GameObject.Find("PlayerHealthPoint");
        if (textObject != null)
        {
            playerHealthText = textObject.GetComponent<TextMeshProUGUI>();
            playerHealthText.text = $"Health {currentHealth}";
        }
        else
        {
            Debug.LogWarning("PlayerHealthPoint not found in scene!");
        }
    }

    public void TakeDamage(int damage)
    {
        FlashWhenHurt();
        
        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage. Current Health: {currentHealth}");

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
    }
    
    private void FlashWhenHurt()
    {
        StartCoroutine(Flash());
    }
  
    private System.Collections.IEnumerator Flash()
    {
        spriteRenderer.color = new Color(1f, 0.2f, 0.2f); // bright red
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        OnPlayerDeath?.Invoke();
        currentHealth = maxHealth;
        StartCoroutine(DieAfterFlash());
        playerHealthText.text = $"Health {currentHealth}";
    }
    
    private System.Collections.IEnumerator DieAfterFlash()
    {
        spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
        yield return new WaitForSeconds(flashDuration);  // Wait for flash to be visible
        Destroy(gameObject);
    }
}