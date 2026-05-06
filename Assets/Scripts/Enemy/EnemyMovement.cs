using UnityEngine;

/// <summary>
/// Mouvement de l'ennemi. L'ennemi avance TOUJOURS jusqu'à sa destruction.
/// Ne s'arrête jamais, même en état vulnérable.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    public Vector3 moveDirection = Vector3.forward;
    public float baseSpeed = 3f;

    [SerializeField] private float actualSpeed;
    [SerializeField] private bool isMoving = true;

    private EnemyBase enemyBase;

    private void Start()
    {
        enemyBase = GetComponent<EnemyBase>();
    }

    public void Initialize(Vector3 direction, float speed)
    {
        moveDirection = direction.normalized;
        baseSpeed = speed;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving) return;

        // L'ennemi s'arrête SEULEMENT quand il est mort ou en train d'exploser
        if (enemyBase != null &&
            (enemyBase.CurrentState == EnemyBase.EnemyState.Dead ||
             enemyBase.CurrentState == EnemyBase.EnemyState.Exploding))
        {
            isMoving = false;
            return;
        }

        // Avance toujours (Moving ET Vulnerable)
        actualSpeed = SpeedMultiplier.Instance != null
            ? SpeedMultiplier.Instance.GetScaledSpeed(baseSpeed)
            : baseSpeed;

        transform.position += moveDirection * actualSpeed * Time.deltaTime;
    }

    public void Stop()
    {
        isMoving = false;
    }
}
