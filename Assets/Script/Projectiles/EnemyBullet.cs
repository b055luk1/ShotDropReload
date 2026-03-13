using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 1;

    void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            
            // --- THE PHASE THROUGH CHECK ---
            if (pc != null && pc.isInvulnerable)
            {
                // If the player is rolling, we do NOTHING. 
                // The bullet will continue moving as if it never hit anything.
                return; 
            }

            // Otherwise, apply damage as normal
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
            }
            Destroy(gameObject);
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            Destroy(gameObject);
        }
    }
}