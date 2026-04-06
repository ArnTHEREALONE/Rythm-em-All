using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Contrôleur principal du joueur. Hub qui orchestre les sous-composants
/// (mouvement, combat, dash, santé) et gère les inputs via le New Input System.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerDash))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerController : MonoBehaviour
{
    [Header("=== Sous-composants ===")]
    public PlayerMovement movement;
    public PlayerCombat combat;
    public PlayerDash dash;
    public PlayerHealth health;

    [Header("=== Configuration ===")]
    public PlayerConfig config;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        // Auto-récupérer les composants si pas assignés
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
        if (dash == null) dash = GetComponent<PlayerDash>();
        if (health == null) health = GetComponent<PlayerHealth>();

        // Créer les input actions
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        // Inputs de mouvement
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        // Inputs de combat
        inputActions.Player.AttackLeft.performed += OnAttackLeft;
        inputActions.Player.AttackRight.performed += OnAttackRight;

        // Input de dash
        inputActions.Player.Dash.performed += OnDash;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.AttackLeft.performed -= OnAttackLeft;
        inputActions.Player.AttackRight.performed -= OnAttackRight;
        inputActions.Player.Dash.performed -= OnDash;

        inputActions.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        if (movement != null)
        {
            // Convertir Vector2 (XY) en Vector3 (XZ) pour un jeu top-down
            movement.SetMoveInput(new Vector3(input.x, 0f, input.y));
        }
    }

    private void OnAttackLeft(InputAction.CallbackContext ctx)
    {
        if (combat != null)
        {
            combat.AttackLeft();
        }
    }

    private void OnAttackRight(InputAction.CallbackContext ctx)
    {
        if (combat != null)
        {
            combat.AttackRight();
        }
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (dash != null && movement != null)
        {
            dash.TryDash(movement.LastMoveDirection);
        }
    }

    /// <summary>
    /// Initialise le joueur avec la config.
    /// </summary>
    public void Initialize(PlayerConfig playerConfig)
    {
        config = playerConfig;

        if (movement != null) movement.Initialize(config);
        if (dash != null) dash.Initialize(config);
        if (health != null) health.Initialize(config);
    }
}
