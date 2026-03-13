using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement (Right Click)")]
    public float recoilForce = 20f; 
    public float recoilCooldown = 0.4f; 
    private float nextRecoilTime = 0f;

    [Header("Rolly Polly (A/D)")]
    public float rollForce = 18f; 
    public float rollCooldown = 0.35f; 
    public float airManeuverability = 1.6f; 
    private float nextRollTime = 0f;
    private bool hasUsedAirRoll = false; 
    private bool isRolling = false; // Prevents shooting during roll

    [Header("Ground Smash (S)")]
    public float smashForce = 28f;

    [Header("Ground Check System")]
    public Transform groundCheck;     
    public float groundCheckRadius = 0.3f;
    public LayerMask environmentLayer; 
    private bool isGrounded;

    [Header("Combat (Left Click)")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 35f;
    public int maxAmmo = 10;
    private int currentAmmo;
    public float reloadTime = 1.2f;
    private bool isReloading = false;

    [Header("Time Slow (Space)")]
    public float slowTimeScale = 0.3f;
    public float maxSlowEnergy = 100f;
    private float currentSlowEnergy;
    private bool canUseSlow = true;

    [Header("Setup References")]
    public Transform armPivot;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentAmmo = maxAmmo;
        currentSlowEnergy = maxSlowEnergy;
        
        rb.freezeRotation = true; 
        rb.linearDamping = 0.8f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        CheckForGround();
        HandleArmRotation();
        HandleTimeSlow();

        // 1. LEFT CLICK: Only fire if NOT reloading AND NOT mid-roll
        if (Input.GetMouseButtonDown(0) && currentAmmo > 0 && !isReloading && !isRolling)
        {
            ShootBullet();
        }

        if (!isReloading)
        {
            // 2. RIGHT CLICK: Consistent Recoil Maneuver
            if (Input.GetMouseButtonDown(1) && Time.time >= nextRecoilTime)
            {
                PushPlayer();
                nextRecoilTime = Time.time + recoilCooldown;
            }

            // 3. A & D: Rolly Polly Logic
            HandleManeuvering();

            // 4. S: Ground Smash
            if (Input.GetKeyDown(KeyCode.S)) rb.AddForce(Vector2.down * smashForce, ForceMode2D.Impulse);
        }

        // Manual Reload
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo) StartCoroutine(Reload());
    }

    void CheckForGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, environmentLayer);
        if (isGrounded)
        {
            hasUsedAirRoll = false;
        }
    }

    void HandleManeuvering()
    {
        float moveInput = 0;
        if (Input.GetKeyDown(KeyCode.A)) moveInput = -1;
        if (Input.GetKeyDown(KeyCode.D)) moveInput = 1;

        if (moveInput != 0)
        {
            // IF ON GROUND: Check cooldown and lockout shooting
            if (isGrounded && Time.time >= nextRollTime)
            {
                nextRollTime = Time.time + rollCooldown;
                StartCoroutine(RollLockout());
                ApplyRoll(moveInput);
            }
            // IF IN AIR: One-time use air maneuver
            else if (!isGrounded && !hasUsedAirRoll)
            {
                hasUsedAirRoll = true; 
                ApplyRoll(moveInput);
            }
        }
    }

    // Disables shooting for the duration of the roll
    IEnumerator RollLockout()
    {
        isRolling = true;
        yield return new WaitForSeconds(rollCooldown);
        isRolling = false;
    }

    void ApplyRoll(float input)
    {
        float currentX = rb.linearVelocity.x;

        // Snappy direction switching
        if ((input > 0 && currentX < 0) || (input < 0 && currentX > 0))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.AddForce(new Vector2(input * rollForce * airManeuverability, 0), ForceMode2D.Impulse);
        }
        else
        {
            rb.AddForce(new Vector2(input * rollForce, 0), ForceMode2D.Impulse);
        }
    }

    void PushPlayer()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // FIXED: Direction is normalized so distance from mouse doesn't change the power
        Vector2 pushDir = ((Vector2)transform.position - (Vector2)mousePos).normalized;
        rb.AddForce(pushDir * recoilForce, ForceMode2D.Impulse);
    }

    void ShootBullet()
    {
        currentAmmo--;
        GameObject b = Instantiate(bulletPrefab, firePoint.position, armPivot.rotation);
        b.GetComponent<Rigidbody2D>().linearVelocity = firePoint.right * bulletSpeed;
        if (currentAmmo <= 0) StartCoroutine(Reload());
    }

    void HandleTimeSlow()
    {
        if (Input.GetKey(KeyCode.Space) && canUseSlow && currentSlowEnergy > 0)
        {
            Time.timeScale = slowTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale; 
            currentSlowEnergy -= 45f * Time.unscaledDeltaTime;
        }
        else
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            if (currentSlowEnergy < maxSlowEnergy) currentSlowEnergy += 20f * Time.unscaledDeltaTime;
        }

        if (currentSlowEnergy <= 0) canUseSlow = false;
        if (currentSlowEnergy >= maxSlowEnergy) canUseSlow = true;
    }

    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    void HandleArmRotation()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)mousePos - (Vector2)armPivot.position;
        armPivot.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        float side = mousePos.x < transform.position.x ? -1 : 1;
        transform.localScale = new Vector3(side, 1, 1);
        armPivot.localScale = new Vector3(1, side, 1);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(groundCheck.position, groundCheckRadius);
        }
    }
}