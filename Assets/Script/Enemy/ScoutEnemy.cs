using UnityEngine;
using System.Collections;

public class ScoutEnemy : MonoBehaviour
{
    public enum ScoutState { Idle, Patrolling, Engaging }
    [Header("State Management")]
    public ScoutState currentState = ScoutState.Idle;
    public float idleTime = 2f;
    private bool isBehaviorTransitioning = false;
    private bool isAlerted = false;
    private bool isDoingAlertHop = false; 

    [Header("Movement & Patrol")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5.5f;
    public Transform groundCheck;
    public float platformDetectDistance = 0.6f;
    public float wallCheckDistance = 0.3f;
    public float jumpForce = 8.5f; 
    public float jumpForwardBoost = 1.3f; 
    public LayerMask environmentLayer;
    private bool movingRight = true;
    private bool isJumping = false; 
    private Rigidbody2D rb;

    [Header("Detection & Cone Settings")]
    public float visualRadius = 12f;   
    public float shootingRadius = 7f; 
    public float alertRadiusBoost = 5f; 
    private float baseVisualRadius;
    [Range(0, 180)]
    public float viewAngle = 80f; 
    public float horizontalThreshold = 1.2f; 
    public LayerMask detectionMask; 
    private Transform player;

    [Header("Alert Visuals")]
    public GameObject exclamationPoint;

    [Header("Combat")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float timeBetweenBursts = 2.0f; 
    public float fireRate = 0.15f;        
    private bool isShooting = false;
    private bool aimLocked = false; 

    [Header("Health & Feedback")]
    public int health = 3;
    public SpriteRenderer spriteRenderer;
    public Color damageColor = Color.red;
    private Color originalColor;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        if (exclamationPoint != null) exclamationPoint.SetActive(false);
        baseVisualRadius = visualRadius;
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, environmentLayer);
        
        if (isGrounded && isJumping && rb.linearVelocity.y <= 0.1f) isJumping = false;
        if (isGrounded && isDoingAlertHop && rb.linearVelocity.y <= 0.1f) isDoingAlertHop = false;

        if (player == null || !player.gameObject.activeInHierarchy) { FindPlayer(); return; }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (isDoingAlertHop || isJumping) return;

        if (distanceToPlayer <= visualRadius && HasLineOfSight())
        {
            if (!isAlerted) StartCoroutine(TriggerAlertSequence());
            currentState = ScoutState.Engaging;
            HandleEngagement(distanceToPlayer);
        }
        else if (currentState == ScoutState.Engaging)
        {
            isAlerted = false;
            visualRadius = baseVisualRadius;
            currentState = ScoutState.Idle;
        }

        if (currentState == ScoutState.Patrolling) PatrolMovement();
        else if (currentState == ScoutState.Idle && !isBehaviorTransitioning) StartCoroutine(IdleWait());
    }

    void HandleEngagement(float distance)
    {
        float xDiff = player.position.x - transform.position.x;
        bool inCone = IsPlayerInCone();
        bool isCentered = Mathf.Abs(xDiff) < horizontalThreshold;

        // 1. Aim the Arm (Always track unless firing)
        if (!aimLocked)
        {
            Vector2 aimDirection = (player.position - firePoint.position).normalized;
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0, 0, angle);
        }

        // 2. SMART FLIP (Twitch Fix)
        // Prevents flipping when player is directly above/below
        if (!inCone && !isCentered)
        {
            if (xDiff > 0 && !movingRight) Flip();
            else if (xDiff < 0 && movingRight) Flip();
        }

        // 3. SHOOTING
        if (distance <= shootingRadius && inCone)
        {
            if (!isShooting) StartCoroutine(ShootBurst());
        }

        // 4. IMPROVED MOVEMENT & REPOSITIONING
        if (!isShooting)
        {
            // If centered but NOT in cone (underneath/above), back up to widen the angle
            if (isCentered && !inCone)
            {
                transform.Translate(Vector2.left * (runSpeed * 0.5f) * Time.deltaTime);
            }
            // Otherwise, chase if too far or move to stay side-by-side
            else if (!isCentered || distance > shootingRadius)
            {
                transform.Translate(Vector2.right * runSpeed * Time.deltaTime);
                CheckForGaps();
            }
            else
            {
                // Stand still only if aligned AND in cone
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
    }

    bool IsPlayerInCone()
    {
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector2.Angle(transform.right, dirToPlayer);
        return (angleToPlayer < viewAngle / 2f) && HasLineOfSight();
    }

    bool HasLineOfSight()
    {
        Vector2 direction = (player.position - firePoint.position).normalized;
        float distance = Vector2.Distance(firePoint.position, player.position);
        RaycastHit2D[] hits = Physics2D.RaycastAll(firePoint.position, direction, distance, detectionMask);
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Environment")) return false; 
            if (hit.collider.CompareTag("Player")) return true;
        }
        return false;
    }

    IEnumerator TriggerAlertSequence()
    {
        isAlerted = true;
        isDoingAlertHop = true;
        rb.linearVelocity = Vector2.zero; // Reset to ensure hop is clean
        rb.linearVelocity = new Vector2(0, 5f); 

        if (exclamationPoint != null)
        {
            exclamationPoint.SetActive(true);
            yield return new WaitForSeconds(0.6f);
            exclamationPoint.SetActive(false);
        }
    }

    IEnumerator ShootBurst()
    {
        isShooting = true;
        aimLocked = true; 
        for (int i = 0; i < 3; i++)
        {
            if (projectilePrefab != null && firePoint != null)
                Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            yield return new WaitForSeconds(fireRate);
        }
        aimLocked = false; 
        yield return new WaitForSeconds(timeBetweenBursts);
        isShooting = false;
    }

    void Flip()
    {
        movingRight = !movingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    void Jump()
    {
        if (rb != null && !isJumping)
        {
            isJumping = true;
            float horizontalDir = movingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(horizontalDir * runSpeed * jumpForwardBoost, jumpForce);
        }
    }

    void CheckForGaps()
    {
        RaycastHit2D groundInfo = Physics2D.Raycast(groundCheck.position, Vector2.down, platformDetectDistance, environmentLayer);
        if (groundInfo.collider == null) Jump();
    }

    void PatrolMovement()
    {
        firePoint.localRotation = Quaternion.identity; 
        transform.Translate(Vector2.right * walkSpeed * Time.deltaTime);
        RaycastHit2D groundInfo = Physics2D.Raycast(groundCheck.position, Vector2.down, platformDetectDistance, environmentLayer);
        Vector2 rayDir = movingRight ? Vector2.right : Vector2.left;
        RaycastHit2D wallInfo = Physics2D.Raycast(groundCheck.position, rayDir, wallCheckDistance, environmentLayer);
        if (groundInfo.collider == null || wallInfo.collider == true) currentState = ScoutState.Idle;
    }

    IEnumerator IdleWait()
    {
        isBehaviorTransitioning = true;
        yield return new WaitForSeconds(idleTime);
        if (currentState != ScoutState.Engaging) Flip();
        currentState = ScoutState.Patrolling;
        isBehaviorTransitioning = false;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        visualRadius = baseVisualRadius + alertRadiusBoost;
        if (!isAlerted) StartCoroutine(TriggerAlertSequence());
        if (spriteRenderer != null) StartCoroutine(FlickerEffect());
        if (health <= 0) Destroy(gameObject);
    }

    IEnumerator FlickerEffect()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visualRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRadius);
        Vector3 leftBoundary = Quaternion.AngleAxis(-viewAngle / 2f, Vector3.forward) * transform.right;
        Vector3 rightBoundary = Quaternion.AngleAxis(viewAngle / 2f, Vector3.forward) * transform.right;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, leftBoundary * shootingRadius);
        Gizmos.DrawRay(transform.position, rightBoundary * shootingRadius);
    }
}