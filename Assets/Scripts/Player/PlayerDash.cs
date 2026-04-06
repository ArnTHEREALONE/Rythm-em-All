using UnityEngine;

/// <summary>
/// Dash à distance fixe. Le cooldown est réduit par la vitesse du jeu.
/// Plus le combo est haut, plus le dash revient vite.
/// </summary>
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

    /// <summary>Si le joueur est actuellement en train de dasher.</summary>
    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Initialise avec la config du joueur.
    /// </summary>
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
        // Cooldown
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // Animation du dash
        if (isDashing)
        {
            dashTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dashTimer / dashDuration);

            // Mouvement lissé (ease-out)
            float smooth = 1f - Mathf.Pow(1f - t, 3f);
            rb.MovePosition(Vector3.Lerp(dashStartPos, dashTargetPos, smooth));

            if (t >= 1f)
            {
                isDashing = false;
            }
        }
    }

    /// <summary>
    /// Tente d'effectuer un dash dans la direction donnée.
    /// </summary>
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

        // Cooldown scalé par la vitesse du jeu
        currentCooldown = SpeedMultiplier.Instance != null
            ? SpeedMultiplier.Instance.GetScaledCooldown(baseCooldown)
            : baseCooldown;
        cooldownTimer = currentCooldown;

        // SFX
        if (dashSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(dashSFX);
        }
    }

    /// <summary>
    /// Retourne le ratio de cooldown restant (0 = prêt, 1 = plein cooldown).
    /// Utile pour l'UI.
    /// </summary>
    public float GetCooldownRatio()
    {
        if (currentCooldown <= 0f) return 0f;
        return Mathf.Clamp01(cooldownTimer / currentCooldown);
    }
}
