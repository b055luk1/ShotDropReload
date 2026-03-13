using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 10;
    public int currentHealth;

    [Header("Visual Feedback")]
    public SpriteRenderer spriteRenderer;
    public Color damageColor = Color.red;
    private Color originalColor;
    private bool isInvulnerable = false;
    public float invulnerabilityTime = 0.5f;

    void Start()
    {
        currentHealth = maxHealth;

        // Auto-assign SpriteRenderer if not set in Inspector
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
        // Prevent taking damage if already hit (brief invulnerability)
        if (isInvulnerable) return;

        currentHealth -= damage;
        Debug.Log("Player Health: " + currentHealth);

        if (currentHealth > 0)
        {
            StartCoroutine(DamageFeedback());
        }
        else
        {
            Die();
        }
    }

    IEnumerator DamageFeedback()
    {
        isInvulnerable = true;
        
        // Flicker effect
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }

        yield return new WaitForSeconds(invulnerabilityTime);
        isInvulnerable = false;
    }

    void Die()
    {
        Debug.Log("Player has died!");
        // You can add a reload scene logic here or a "Game Over" screen
        // For now, let's just disable the player
        gameObject.SetActive(false);
    }
}
