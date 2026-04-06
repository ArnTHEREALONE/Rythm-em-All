using UnityEngine;

/// <summary>
/// Mouvement en ligne droite d'un ennemi depuis le spawner vers le centre.
/// La vitesse est scalée par le multiplicateur global en temps réel.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Direction de mouvement (définie par le spawner)")]
    public Vector3 moveDirection = Vector3.forward;

    [Tooltip("Vitesse de base (depuis EnemyData.baseMoveSpeed)")]
    public float baseSpeed = 3f;

    [Header("=== État (debug) ===")]
    [SerializeField] private float actualSpeed;
    [SerializeField] private bool isMoving = true;

    private EnemyBase enemyBase;

    private void Start()
    {
        enemyBase = GetComponent<EnemyBase>();
    }

    /// <summary>
    /// Initialise le mouvement avec une direction et une vitesse de base.
    /// </summary>
    public void Initialize(Vector3 direction, float speed)
    {
        moveDirection = direction.normalized;
        baseSpeed = speed;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving) return;

        // Ne bouge plus si l'ennemi n'est pas dans l'état Moving
        if (enemyBase != null &&
            enemyBase.CurrentState != EnemyBase.EnemyState.Moving &&
            enemyBase.CurrentState != EnemyBase.EnemyState.Spawning)
        {
            isMoving = false;
            return;
        }

        // Vitesse scalée par le multiplicateur global
        actualSpeed = SpeedMultiplier.Instance != null
            ? SpeedMultiplier.Instance.GetScaledSpeed(baseSpeed)
            : baseSpeed;

        // Déplacement
        transform.position += moveDirection * actualSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Arrête le mouvement.
    /// </summary>
    public void Stop()
    {
        isMoving = false;
    }
}
