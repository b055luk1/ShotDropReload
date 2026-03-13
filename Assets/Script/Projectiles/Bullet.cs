using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Combat Stats")]
    public float damage = 25f; 
    public int maxBounces = 3; 

    private int currentBounces = 0;
    private Rigidbody2D rb;
    private Vector2 lastVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Safety: If the bullet doesn't hit anything, destroy it after 5 seconds
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        // Track velocity every frame for accurate bounce calculations
        lastVelocity = rb.linearVelocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. HIT AN ENEMY
        // Look for the EnemyHealth script on the object we hit
        EnemyHealth enemy = collision.gameObject.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage, transform.position);
            Destroy(gameObject); // Bullets don't bounce off enemies
            return;
        }

        // 2. HIT THE ENVIRONMENT (Walls/Floors)
        // Check if the object is on the "Environment" layer OR has the "Bouncy" tag
        bool isEnvironment = collision.gameObject.layer == LayerMask.NameToLayer("Environment");
        bool isBouncy = collision.gameObject.CompareTag("Bouncy");

        if (isBouncy || isEnvironment)
        {
            if (currentBounces < maxBounces)
            {
                currentBounces++;

                // Calculate the reflection (ricochet)
                Vector2 surfaceNormal = collision.GetContact(0).normal;
                Vector2 reflectDir = Vector2.Reflect(lastVelocity.normalized, surfaceNormal);

                // Re-apply velocity and rotate bullet to face the new direction
                rb.linearVelocity = reflectDir * lastVelocity.magnitude;
                float angle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                // Max bounces reached, bullet breaks
                Destroy(gameObject);
            }
        }
        else
        {
            // 3. HIT ANYTHING ELSE
            // If it's not an enemy and not part of the environment, just destroy
            Destroy(gameObject);
        }
    }
}