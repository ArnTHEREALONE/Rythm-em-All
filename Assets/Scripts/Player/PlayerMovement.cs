using UnityEngine;

/// <summary>
/// Mouvement du joueur via Rigidbody. ZQSD / Left Joystick.
/// La vitesse est multipliée par le gameSpeed global.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Vitesse de base du joueur")]
    public float baseSpeed = 8f;

    [Header("=== État (debug) ===")]
    [SerializeField] private Vector3 moveInput;
    [SerializeField] private float actualSpeed;

    private Rigidbody rb;

    /// <summary>
    /// Dernière direction de déplacement non-nulle (pour le dash).
    /// </summary>
    public Vector3 LastMoveDirection { get; private set; } = Vector3.forward;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Configuration du Rigidbody pour un jeu top-down
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    /// <summary>
    /// Initialise avec la config du joueur.
    /// </summary>
    public void Initialize(PlayerConfig config)
    {
        if (config != null)
        {
            baseSpeed = config.moveSpeed;
        }
    }

    /// <summary>
    /// Appelé par PlayerController quand l'input de mouvement change.
    /// </summary>
    public void SetMoveInput(Vector3 input)
    {
        moveInput = input;
        if (moveInput.magnitude > 1f)
            moveInput.Normalize();

        // Sauvegarder la dernière direction non-nulle
        if (moveInput.sqrMagnitude > 0.01f)
            LastMoveDirection = moveInput.normalized;
    }

    private void FixedUpdate()
    {
        // Vitesse scalée par le multiplicateur global
        actualSpeed = SpeedMultiplier.Instance != null
            ? SpeedMultiplier.Instance.GetScaledSpeed(baseSpeed)
            : baseSpeed;

        // Appliquer le mouvement via le Rigidbody
        Vector3 velocity = moveInput * actualSpeed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
    }
}
