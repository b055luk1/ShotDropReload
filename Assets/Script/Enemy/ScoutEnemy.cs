using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public float health = 100f;
    public Color flashColor = Color.white;
    public float knockbackResist = 5f;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Color originalColor;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        originalColor = sr.color;
    }

    public void TakeDamage(float amount, Vector2 sourcePos)
    {
        health -= amount;
        
        // Push back logic
        if(rb)
        {
            Vector2 pushDir = ((Vector2)transform.position - sourcePos).normalized;
            rb.AddForce(pushDir * knockbackResist, ForceMode2D.Impulse);
        }

        StartCoroutine(Flash());
        if (health <= 0) Destroy(gameObject);
    }

    IEnumerator Flash()
    {
        sr.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        sr.color = originalColor;
    }
}