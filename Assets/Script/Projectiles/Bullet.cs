using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 1;
    public float lifeTime = 3f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Auto-destroy if it misses everything
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Consistent forward movement
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // 1. Check if we hit the Scout (Enemy)
        ScoutEnemy enemy = hitInfo.GetComponentInParent<ScoutEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject); 
            return;
        }

        // 2. Bouncy / Ricochet Logic
        // If it hits the Environment or something tagged "Bouncy"
        if (hitInfo.CompareTag("Bouncy") || hitInfo.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            // Simple flip for a ricochet effect
            transform.Rotate(0, 0, 180f); 
            // Optional: Destroy(gameObject); // Uncomment this if you DON'T want it to bounce
        }
    }
}