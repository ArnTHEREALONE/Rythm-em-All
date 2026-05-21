using UnityEngine;

/// <summary>
/// Manages the fill-based timing indicator on an enemy prefab.
/// A child SpriteRenderer grows uniformly from scale 0 to 1 over a configurable number of beats,
/// then flashes white when the target beat time is reached.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyTimingIndicator : MonoBehaviour
{
    [Header("=== Fill Indicator ===")]
    [Tooltip("The fill SpriteRenderer that grows from center. Create as a child of the enemy.")]
    public SpriteRenderer fillSprite;

    [Tooltip("How many beats before targetBeatTime the fill animation starts.")]
    public float fillBeatsBeforeTarget = 4f;

    [Tooltip("Color of the fill sprite.")]
    public Color fillColor = Color.red;

    [Tooltip("Flash color when timing is reached.")]
    public Color flashColor = Color.white;

    [Tooltip("Duration of the flash in seconds.")]
    public float flashDuration = 0.1f;

    // --- Internal state ---
    private EnemyBase enemyBase;
    private bool fillStarted;
    private bool hasFlashed;
    private float flashTimer;
    private bool isFlashing;

    private void Awake()
    {
        enemyBase = GetComponent<EnemyBase>();
    }

    private void Start()
    {
        // Initialize fill sprite to invisible / zero scale
        if (fillSprite != null)
        {
            fillSprite.transform.localScale = new Vector3(0f, 0f, 1f);
            fillSprite.color = fillColor;
        }
    }

    private void Update()
    {
        if (fillSprite == null || BeatManager.Instance == null) return;

        float currentBeat = BeatManager.Instance.CurrentBeat;
        float targetBeat = enemyBase.TargetBeatTime;
        float fillStartBeat = targetBeat - fillBeatsBeforeTarget;

        // --- Growing phase ---
        if (!fillStarted)
        {
            if (currentBeat >= fillStartBeat)
            {
                fillStarted = true;
            }
            else
            {
                return;
            }
        }

        if (!hasFlashed)
        {
            // Calculate fill progress: 0 at fillStartBeat, 1 at targetBeat
            float progress = Mathf.Clamp01((currentBeat - fillStartBeat) / fillBeatsBeforeTarget);
            fillSprite.transform.localScale = new Vector3(progress, progress, 1f);

            // Check if we've reached the target beat
            if (currentBeat >= targetBeat)
            {
                // Fill complete — trigger flash
                fillSprite.transform.localScale = new Vector3(1f, 1f, 1f);
                StartFlash();
            }
        }

        // --- Flash phase ---
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                EndFlash();
            }
        }
    }

    /// <summary>
    /// Starts the white flash on both the fill sprite and the enemy's main renderer.
    /// </summary>
    private void StartFlash()
    {
        hasFlashed = true;
        isFlashing = true;
        flashTimer = flashDuration;

        fillSprite.color = flashColor;

        if (enemyBase.mainRenderer != null)
        {
            enemyBase.mainRenderer.material.color = flashColor;
        }
    }

    /// <summary>
    /// Ends the flash and restores the fill sprite color.
    /// The main renderer color is NOT restored here because EnemyBase handles its own color logic.
    /// </summary>
    private void EndFlash()
    {
        isFlashing = false;
        fillSprite.color = fillColor;
    }
}
