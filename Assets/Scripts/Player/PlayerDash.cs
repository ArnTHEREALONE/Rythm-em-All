using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDash : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Distance fixe parcourue lors du dash")]
    public float dashDistance = 5f;

    [Tooltip("Cooldown de base en secondes (divisé par gameSpeed)")]
    public float baseCooldown = 1f;

    [Tooltip("Durée du dash en secondes (le mouvement est lissé sur cette durée)")]
    public float dashDuration = 0.15f;

    [Header("=== SFX ===")]
    public AudioClip dashSFX;

    [Header("=== État (debug) ===")]
    [SerializeField] private bool isDashing;
    [SerializeField] private float cooldownTimer;
    [SerializeField] private float currentCooldown;

    private Rigidbody rb;
    private Vector3 dashDirection;
    private float dashTimer;
    private Vector3 dashStartPos;
    private Vector3 dashTargetPos;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(PlayerConfig config)
    {
        if (config != null)
        {
            dashDistance = config.dashDistance;
            baseCooldown = config.dashCooldown;
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dashTimer / dashDuration);

            float smooth = 1f - Mathf.Pow(1f - t, 3f);
            rb.MovePosition(Vector3.Lerp(dashStartPos, dashTargetPos, smooth));

            if (t >= 1f)
            {
                isDashing = false;
            }
        }
    }

    public void TryDash(Vector3 direction)
    {
        if (isDashing) return;
        if (cooldownTimer > 0f) return;
        if (direction.sqrMagnitude < 0.01f) return;

        dashDirection = direction.normalized;
        dashStartPos = transform.position;
        dashTargetPos = dashStartPos + dashDirection * dashDistance;
        dashTimer = 0f;
        isDashing = true;

        currentCooldown = SpeedMultiplier.Instance != null
            ? SpeedMultiplier.Instance.GetScaledCooldown(baseCooldown)
            : baseCooldown;
        cooldownTimer = currentCooldown;

        if (dashSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(dashSFX);
        }
    }

    public float GetCooldownRatio()
    {
        if (currentCooldown <= 0f) return 0f;
        return Mathf.Clamp01(cooldownTimer / currentCooldown);
    }
}
