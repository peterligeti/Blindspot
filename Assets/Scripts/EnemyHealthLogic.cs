using System;
using UnityEngine;

public class EnemyHealthLogic : MonoBehaviour
{
    [SerializeField] int maxHealth = 3;
    private int currentHealth;

    public static event Action<GameObject> OnOneEnemyKilled;

    private SpriteRenderer spriteRenderer;
    [SerializeField] float flashDuration = 0.1f;
    private Color originalColor;

    void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
        FlashWhenHurt();
        
        currentHealth -= damage;
        Debug.Log($"Enemy took {damage} damage. Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public int GetCurrentHealth() // Ai logic uses this to decide if it should do low health flee 
    {
        return currentHealth;
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
        Debug.Log("Enemy died!");
        OnOneEnemyKilled?.Invoke(gameObject); // Pass this enemy to listeners
        StartCoroutine(DieAfterFlash());
    }

    private System.Collections.IEnumerator DieAfterFlash()
    {
        spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
        yield return new WaitForSeconds(flashDuration);
        Destroy(gameObject);
    }
}