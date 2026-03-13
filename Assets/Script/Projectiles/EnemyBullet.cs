using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 12f;
    public int damage = 1;
    public float lifeSpan = 3f;

    void Start()
    {
        Destroy(gameObject, lifeSpan);
    }

    void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // 1. Check if we hit the Player
        PlayerHealth player = hitInfo.GetComponent<PlayerHealth>();

        if (player != null)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }
        
        // 2. Check if we hit the Ground/Walls
        if (hitInfo.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}