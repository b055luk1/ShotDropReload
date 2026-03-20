using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 10;
    public int currentHealth;

    [Header("Visual Feedback")]
    public SpriteRenderer bodySprite;
    public SpriteRenderer armSprite;
    public Color damageColor = Color.red;
    private Color originalColor;
    
    [Header("Invulnerability & Flinch")]
    public float invulnerabilityTime = 0.5f;
    public float knockbackForce = 10f;
    private bool isHitInvulnerable = false;
    
    private PlayerController controller;
    private Rigidbody2D rb;

    void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();

        if (bodySprite == null) bodySprite = GetComponent<SpriteRenderer>();
        if (bodySprite != null) originalColor = bodySprite.color;
    }

    public void TakeDamage(int damage, Vector2 hitSource)
    {
        // Check PlayerController to see if we are currently Roly-Polying
        bool isRolling = (controller != null && controller.isInvulnerable);
        
        if (isHitInvulnerable || isRolling) return;

        currentHealth -= damage;

        if (rb != null)
        {
            Vector2 knockbackDir = (transform.position - (Vector3)hitSource).normalized;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(knockbackDir.x, 0.4f) * knockbackForce, ForceMode2D.Impulse);
        }

        if (currentHealth > 0) StartCoroutine(DamageFeedback());
        else Die();
    }

    IEnumerator DamageFeedback()
    {
        isHitInvulnerable = true;
        SetSpritesColor(damageColor);
        yield return new WaitForSeconds(0.1f);
        SetSpritesColor(originalColor);
        yield return new WaitForSeconds(invulnerabilityTime);
        isHitInvulnerable = false;
    }

    void SetSpritesColor(Color c)
    {
        if (bodySprite != null) bodySprite.color = c;
        if (armSprite != null) armSprite.color = c;
    }

    void Die()
    {
        gameObject.SetActive(false);
    }
}