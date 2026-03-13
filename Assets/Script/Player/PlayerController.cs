using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("The Arm (For Aiming)")]
    public Transform armTransform; 
    public Transform firePoint;    
    public GameObject bulletPrefab;

    [Header("Roly Poly (A & D Keys)")]
    public float rollForce = 14f;
    public float rollJumpForce = 5f; // Small hop height
    public float rollDuration = 0.4f;
    public float rollCooldown = 0.2f;
    public bool isInvulnerable = false;
    private bool isRolling = false;
    private bool canRoll = true;
    private bool hasAirRolled = false;

    [Header("Fixed Recoil (Right Click)")]
    public float recoilForce = 20f; 
    public float recoilCooldown = 0.3f;
    private bool canRecoil = true;

    [Header("Ground Smash (S Key)")]
    public float smashDownForce = 30f;
    public float smashExplosionRadius = 4f;
    public int smashDamage = 2;
    private bool isSmashing = false;

    [Header("Physics & Visuals")]
    public Rigidbody2D rb;
    public SpriteRenderer bodySprite;
    public SpriteRenderer armSprite;
    public Transform groundCheck;
    public LayerMask environmentLayer;
    private bool isGrounded;
    private bool isFacingRight = true;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        CheckEnvironment();
        RotateArmTowardMouse();

        // --- ROLY POLY LOGIC ---
        if (canRoll && !isRolling)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
            {
                float direction = Input.GetKeyDown(KeyCode.A) ? -1f : 1f;

                if (isGrounded)
                {
                    StartCoroutine(PerformRolyPoly(direction));
                }
                else if (!hasAirRolled)
                {
                    hasAirRolled = true;
                    StartCoroutine(PerformRolyPoly(direction));
                }
            }
        }

        // --- RECOIL MANEUVER ---
        if (Input.GetMouseButtonDown(1) && canRecoil && !isRolling)
        {
            StartCoroutine(PerformRecoil());
        }

        // --- GROUND SMASH ---
        if (Input.GetKeyDown(KeyCode.S) && !isGrounded && !isSmashing)
        {
            StartCoroutine(PerformGroundSmash());
        }

        // --- SHOOTING ---
        if (Input.GetMouseButtonDown(0))
        {
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }

        // --- SLOW MOTION ---
        Time.timeScale = Input.GetKey(KeyCode.Space) ? 0.4f : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        UpdateFacingDirection();
    }

    void CheckEnvironment()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, environmentLayer);
        if (isGrounded && !wasGrounded) hasAirRolled = false;
    }

    IEnumerator PerformRolyPoly(float direction)
    {
        canRoll = false; 
        isRolling = true; 
        isInvulnerable = true;
        
        // --- THE HOP LOGIC ---
        // We apply horizontal rollForce AND a small vertical rollJumpForce
        rb.linearVelocity = new Vector2(direction * rollForce, rollJumpForce);
        
        yield return new WaitForSeconds(rollDuration);
        
        isRolling = false; 
        isInvulnerable = false;
        yield return new WaitForSeconds(rollCooldown);
        canRoll = true;
    }

    IEnumerator PerformRecoil()
    {
        canRecoil = false;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -Camera.main.transform.position.z; 
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);
        worldMousePos.z = 0;

        Vector2 recoilDirection = (transform.position - worldMousePos).normalized;
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(recoilDirection * recoilForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(recoilCooldown);
        canRecoil = true;
    }

    void RotateArmTowardMouse()
    {
        if (armTransform == null) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
        Vector2 direction = (mousePos - armTransform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        armTransform.rotation = Quaternion.Euler(0, 0, angle);

        if (armSprite != null)
            armSprite.flipY = (mousePos.x < armTransform.position.x);
    }

    IEnumerator PerformGroundSmash()
    {
        isSmashing = true; 
        rb.linearVelocity = new Vector2(0, -smashDownForce);
        while (!isGrounded) yield return null;
        
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, smashExplosionRadius);
        foreach (var e in enemies) {
            if (e.CompareTag("Enemy")) e.GetComponent<ScoutEnemy>()?.TakeDamage(smashDamage);
        }
        yield return new WaitForSeconds(0.2f); 
        isSmashing = false;
    }

    void UpdateFacingDirection()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
        if (mousePos.x > transform.position.x && !isFacingRight) { isFacingRight = true; bodySprite.flipX = false; }
        else if (mousePos.x < transform.position.x && isFacingRight) { isFacingRight = false; bodySprite.flipX = true; }
    }
}