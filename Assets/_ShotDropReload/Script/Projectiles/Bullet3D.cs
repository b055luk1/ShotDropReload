using UnityEngine;

public class Bullet3D : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 1;
    public float lifeTime = 3f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Auto-destroy if it misses everything
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Consistent forward movement
        transform.Translate(Vector3.right * speed * Time.deltaTime);
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
    }
}