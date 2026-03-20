using UnityEngine;
using System.Collections;

public class PlayerController3D : MonoBehaviour
{
    [Header("The Arm & Shooting")]
    public Transform armTransform;

    public Transform armVisualTransform;
    public Transform firePoint;
    public GameObject bulletPrefab;
    public int maxAmmo = 12;
    public int currentAmmo;
    public float reloadTime = 1.5f;
    private bool isReloading = false;

    [Header("Roly Poly (A & D Keys)")]
    public float rollForce = 14f;

    public float rollJumpForce = 5f;
    public float rollDuration = 0.4f;
    public float rollCooldown = 0.2f;

    [HideInInspector]
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
    public Rigidbody rb;

    public Transform bodyTransform;
    public Transform groundCheck;
    public LayerMask environmentLayer;
    private bool isGrounded;
    public bool isFacingRight = true;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        CheckEnvironment();
        RotateArmTowardMouse();

        // --- SLOW MOTION ---
        if (Input.GetKey(KeyCode.Space))
        {
            Time.timeScale = 0.4f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
        else
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
        }

        // --- SHOOTING ---
        if (Input.GetMouseButtonDown(0) && currentAmmo > 0 && !isReloading && !isRolling)
        {
            Shoot();
        }
        else if ((Input.GetMouseButtonDown(0) && currentAmmo <= 0) || Input.GetKeyDown(KeyCode.R))
        {
            if (!isReloading && currentAmmo < maxAmmo) StartCoroutine(Reload());
        }

        // --- ROLY POLY ---
        if (canRoll && !isRolling)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
            {
                float direction = Input.GetKeyDown(KeyCode.A) ? -1f : 1f;
                if (isGrounded || !hasAirRolled)
                {
                    if (!isGrounded) hasAirRolled = true;
                    StartCoroutine(PerformRolyPoly(direction));
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && canRecoil && !isRolling) StartCoroutine(PerformRecoil());
        if (Input.GetKeyDown(KeyCode.S) && !isGrounded && !isSmashing) StartCoroutine(PerformGroundSmash());

        UpdateFacingDirection();
    }

    void Shoot()
    {
        currentAmmo--;
        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }

    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    IEnumerator PerformRolyPoly(float direction)
    {
        canRoll = false;
        isRolling = true;
        isInvulnerable = true;
        rb.linearVelocity = new Vector3(direction * rollForce, rollJumpForce, 0);
        yield return new WaitForSeconds(rollDuration);
        isRolling = false;
        isInvulnerable = false;
        yield return new WaitForSeconds(rollCooldown);
        canRoll = true;
    }

    void CheckEnvironment()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.OverlapSphere(groundCheck.position, 0.2f, environmentLayer).Length > 0;
        if (isGrounded && !wasGrounded) hasAirRolled = false;
    }

    IEnumerator PerformRecoil()
    {
        canRecoil = false;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
            -Camera.main.transform.position.z));
        Vector2 recoilDirection = (transform.position - mousePos).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(recoilDirection * recoilForce, ForceMode.Impulse);
        yield return new WaitForSeconds(recoilCooldown);
        canRecoil = true;
    }

    void RotateArmTowardMouse()
    {
        if (armTransform == null) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
            -Camera.main.transform.position.z));
        Vector2 direction = (mousePos - armTransform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var facingAngle = mousePos.x < armTransform.position.x ? 180 : 0;
        armTransform.localRotation = Quaternion.Euler(0, 0, angle);
        armVisualTransform.localRotation = Quaternion.Euler(facingAngle, 0, 0);
    }

    IEnumerator PerformGroundSmash()
    {
        isSmashing = true;
        rb.linearVelocity = new Vector2(0, -smashDownForce);
        while (!isGrounded) yield return null;

        Collider[] enemies = Physics.OverlapSphere(transform.position, smashExplosionRadius);
        foreach (var e in enemies)
        {
            if (e.CompareTag("Enemy")) e.GetComponent<ScoutEnemy>()?.TakeDamage(smashDamage);
        }

        yield return new WaitForSeconds(0.2f);
        isSmashing = false;
    }

    void UpdateFacingDirection()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
            -Camera.main.transform.position.z));
        if (mousePos.x > transform.position.x && !isFacingRight)
        {
            isFacingRight = true;
        }
        else if (mousePos.x < transform.position.x && isFacingRight)
        {
            isFacingRight = false;
        }

        var bodyRotationEulerAngle = bodyTransform.localRotation.eulerAngles;
        bodyTransform.localRotation =
            Quaternion.Euler(isFacingRight ? 0 : 180, bodyRotationEulerAngle.y, bodyRotationEulerAngle.z);
    }
}