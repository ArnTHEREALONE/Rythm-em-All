using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Singleton managing arena-wide visual effects: disco floor shader, screen flash, and camera glitch.
/// Attach to a persistent manager GameObject in the scene.
/// </summary>
public class ArenaVFX : MonoBehaviour
{
    public static ArenaVFX Instance { get; private set; }

    [Header("=== Disco Damier (Floor Shader) ===")]
    [Tooltip("The floor plane Renderer whose material has _BeatPhase and _FlashColor/_FlashIntensity properties.")]
    [SerializeField] private Renderer floorRenderer;

    [Header("=== Screen Flash ===")]
    [Tooltip("Fullscreen UI Image overlay (raycast OFF). Used for red border flash on enemy auto-destruct.")]
    [SerializeField] private Image screenFlashImage;

    [Header("=== Glitch (Camera Shake) ===")]
    [Tooltip("Camera to jitter. If left empty, Camera.main is used.")]
    [SerializeField] private Camera targetCamera;

    // --- Internal state ---
    private Material floorMat;
    private Coroutine floorFlashCoroutine;
    private Coroutine screenFlashCoroutine;
    private Coroutine glitchCoroutine;
    private Vector3 cameraOriginalPosition;

    // Shader property IDs (cached for performance)
    private static readonly int BeatPhaseID = Shader.PropertyToID("_BeatPhase");
    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashIntensityID = Shader.PropertyToID("_FlashIntensity");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Cache the floor material instance
        if (floorRenderer != null)
        {
            floorMat = floorRenderer.material;
        }

        // Fallback to main camera
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // Ensure screen flash starts fully transparent
        if (screenFlashImage != null)
        {
            Color c = screenFlashImage.color;
            c.a = 0f;
            screenFlashImage.color = c;
        }
    }

    private void Update()
    {
        UpdateFloorBeatPhase();
    }

    // ========================================================================
    // DISCO DAMIER — Floor shader beat phase
    // ========================================================================

    /// <summary>
    /// Sends the current beat value to the floor shader every frame
    /// so the Shader Graph can pulse in sync with the music.
    /// </summary>
    private void UpdateFloorBeatPhase()
    {
        if (floorMat == null || BeatManager.Instance == null) return;

        floorMat.SetFloat(BeatPhaseID, BeatManager.Instance.CurrentBeat);
    }

    /// <summary>
    /// Flashes the disco floor red by setting _FlashColor to red and _FlashIntensity to 1,
    /// then lerps the intensity back to 0 over the given duration.
    /// </summary>
    /// <param name="duration">Time in seconds for the flash to fade out.</param>
    public void FlashDamierRed(float duration)
    {
        if (floorMat == null) return;

        if (floorFlashCoroutine != null)
            StopCoroutine(floorFlashCoroutine);

        floorFlashCoroutine = StartCoroutine(FlashDamierRedCoroutine(duration));
    }

    private IEnumerator FlashDamierRedCoroutine(float duration)
    {
        floorMat.SetColor(FlashColorID, Color.red);
        floorMat.SetFloat(FlashIntensityID, 1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            floorMat.SetFloat(FlashIntensityID, 1f - t);
            yield return null;
        }

        floorMat.SetFloat(FlashIntensityID, 0f);
        floorFlashCoroutine = null;
    }

    // ========================================================================
    // SCREEN FLASH — Red overlay
    // ========================================================================

    /// <summary>
    /// Flashes the screen-wide red overlay by setting alpha to 0.5,
    /// then lerping it back to 0 over the given duration.
    /// </summary>
    /// <param name="duration">Time in seconds for the flash to fade out.</param>
    public void FlashScreenRed(float duration)
    {
        if (screenFlashImage == null) return;

        if (screenFlashCoroutine != null)
            StopCoroutine(screenFlashCoroutine);

        screenFlashCoroutine = StartCoroutine(FlashScreenRedCoroutine(duration));
    }

    private IEnumerator FlashScreenRedCoroutine(float duration)
    {
        Color c = new Color(1f, 0f, 0f, 0.5f);
        screenFlashImage.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            c.a = Mathf.Lerp(0.5f, 0f, t);
            screenFlashImage.color = c;
            yield return null;
        }

        c.a = 0f;
        screenFlashImage.color = c;
        screenFlashCoroutine = null;
    }

    // ========================================================================
    // GLITCH — Camera jitter
    // ========================================================================

    /// <summary>
    /// Briefly jitters the camera with small random offsets to simulate a glitch effect,
    /// then restores the original position.
    /// </summary>
    /// <param name="duration">How long the jitter lasts in seconds.</param>
    /// <param name="intensity">Maximum offset distance on each axis.</param>
    public void GlitchEffect(float duration, float intensity)
    {
        if (targetCamera == null) return;

        if (glitchCoroutine != null)
            StopCoroutine(glitchCoroutine);

        glitchCoroutine = StartCoroutine(GlitchCoroutine(duration, intensity));
    }

    private IEnumerator GlitchCoroutine(float duration, float intensity)
    {
        cameraOriginalPosition = targetCamera.transform.position;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Random offset on X and Z (top-down view, camera looks down Y axis)
            float offsetX = Random.Range(-intensity, intensity);
            float offsetZ = Random.Range(-intensity, intensity);
            targetCamera.transform.position = cameraOriginalPosition + new Vector3(offsetX, 0f, offsetZ);

            yield return null;
        }

        // Restore original position
        targetCamera.transform.position = cameraOriginalPosition;
        glitchCoroutine = null;
    }
}
