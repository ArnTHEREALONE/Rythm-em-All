using UnityEngine;

/// <summary>
/// Gère le déplacement des ennemis : direction, vitesse, et zigzag optionnel.
/// Le zigzag est un mouvement sinusoïdal latéral paramétrable via EnemyData.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    public Vector3 moveDirection = Vector3.forward;
    public float baseSpeed = 3f;

    [SerializeField] private float actualSpeed;
    [SerializeField] private bool isMoving = true;

    private EnemyBase enemyBase;

    // Zigzag
    private bool zigzagEnabled;
    private float zigzagAmplitude;
    private float zigzagFrequency;
    private Vector3 zigzagAxis; // axe perpendiculaire à la direction de déplacement
    private float zigzagTimer;

    private void Start()
    {
        enemyBase = GetComponent<EnemyBase>();
    }

    public void Initialize(Vector3 direction, float speed)
    {
        moveDirection = direction.normalized;
        baseSpeed = speed;
        isMoving = true;

        // Calculer l'axe de zigzag : perpendiculaire à la direction sur le plan XZ
        zigzagAxis = Vector3.Cross(moveDirection, Vector3.up).normalized;
        if (zigzagAxis.sqrMagnitude < 0.01f)
            zigzagAxis = Vector3.right; // fallback si la direction est verticale

        zigzagTimer = 0f;
    }

    /// <summary>
    /// Configure le zigzag depuis l'EnemyData. Appelé par EnemySpawner après Initialize.
    /// </summary>
    public void SetZigzag(bool enabled, float amplitude, float frequency)
    {
        zigzagEnabled = enabled;
        zigzagAmplitude = amplitude;
        zigzagFrequency = frequency;
    }

    private void Update()
    {
        if (!isMoving) return;

        // Stop si mort ou explosion
        if (enemyBase != null &&
            (enemyBase.CurrentState == EnemyBase.EnemyState.Dead ||
             enemyBase.CurrentState == EnemyBase.EnemyState.Exploding))
        {
            isMoving = false;
            return;
        }

        // Vitesse avec multiplicateur de jeu
        actualSpeed = SpeedMultiplier.Instance != null
            ? SpeedMultiplier.Instance.GetScaledSpeed(baseSpeed)
            : baseSpeed;

        // Déplacement principal
        Vector3 displacement = moveDirection * actualSpeed * Time.deltaTime;

        // Zigzag latéral (sinusoïdal)
        if (zigzagEnabled && zigzagAmplitude > 0f && zigzagFrequency > 0f)
        {
            zigzagTimer += Time.deltaTime;

            // On utilise la dérivée du sinus pour le déplacement latéral frame-à-frame
            // Cela donne un mouvement fluide et sinusoïdal
            float lateralSpeed = zigzagAmplitude * zigzagFrequency * 2f * Mathf.PI
                * Mathf.Cos(zigzagTimer * zigzagFrequency * 2f * Mathf.PI);

            displacement += zigzagAxis * lateralSpeed * Time.deltaTime;
        }

        transform.position += displacement;
    }

    public void Stop()
    {
        isMoving = false;
    }
}
