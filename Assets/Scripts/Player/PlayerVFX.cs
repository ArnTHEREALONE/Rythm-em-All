using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Centralise tous les effets visuels du joueur.
/// S'abonne aux événements de PlayerDash, PlayerCombat, PlayerHealth, PlayerMovement.
/// Crée automatiquement les ParticleSystems, TrailRenderer, et UI nécessaires.
/// </summary>
public class PlayerVFX : MonoBehaviour
{
    [Header("=== Références (auto-détectées) ===")]
    public PlayerDash dash;
    public PlayerCombat combat;
    public PlayerHealth health;
    public PlayerMovement movement;
    public Renderer playerRenderer;

    // ─── DASH ────────────────────────────────────────────
    [Header("=== Dash — Flash ===")]
    public Color dashFlashColor = Color.white;
    public float dashFlashDuration = 0.06f;

    [Header("=== Dash — Trail ===")]
    public Color trailStartColor = new Color(1f, 1f, 1f, 0.8f);
    public Color trailEndColor = new Color(1f, 1f, 1f, 0f);
    public float trailTime = 0.2f;
    public float trailWidth = 0.3f;

    [Header("=== Dash — Dust ===")]
    public Color dustColor = new Color(0.8f, 0.7f, 0.5f, 0.6f);
    public int dustBurstCount = 12;

    [Header("=== Dash — Cooldown UI ===")]
    public Color cdSliderColor = new Color(0.5f, 0.8f, 1f, 0.8f);
    public Color cdReadyFlashColor = Color.white;

    [Header("=== Dash — Jitter ===")]
    [Tooltip("Amplitude de la vibration pendant le cooldown")]
    public float jitterAmplitude = 0.03f;
    public float jitterFrequency = 40f;

    // ─── ATTACK ──────────────────────────────────────────
    [Header("=== Attack — Shockwave ===")]
    public Color shockwaveColor = new Color(1f, 1f, 1f, 0.4f);
    public float shockwaveDuration = 0.15f;

    // ─── MOVEMENT ────────────────────────────────────────
    [Header("=== Movement — Sparkles ===")]
    public Color sparkleColor = new Color(1f, 0.9f, 0.6f, 0.5f);
    public float sparkleRatePerUnit = 8f;

    // ─── HEAL ────────────────────────────────────────────
    [Header("=== Heal — Active (on kill) ===")]
    public Color healActiveColor = new Color(0.3f, 1f, 0.5f, 0.7f);
    public int healActiveBurstCount = 20;

    [Header("=== Heal — Passive ===")]
    public Color healPassiveColor = new Color(0.7f, 0.9f, 1f, 0.4f);

    [Header("=== HP Full — Flash ===")]
    public Color hpFullFlashColor = new Color(0.2f, 1f, 0.4f, 1f);
    public float hpFullFlashDuration = 0.15f;

    [Header("=== Damage — Flash ===")]
    public Color damageFlashColor = new Color(1f, 0.15f, 0.15f, 1f);
    public float damageFlashDuration = 0.12f;

    // ─── Internals ───────────────────────────────────────
    private Color originalColor;
    private TrailRenderer trail;
    private ParticleSystem dustPS;
    private ParticleSystem sparklePS;
    private ParticleSystem healActivePS;
    private ParticleSystem healPassivePS;
    private GameObject shockwaveGO;
    private SpriteRenderer shockwaveSR;

    // Dash cooldown UI
    private Canvas cdCanvas;
    private Slider cdSlider;
    private Image cdFillImage;
    private CanvasGroup cdCanvasGroup;

    private Coroutine flashCoroutine;
    private Coroutine shockwaveCoroutine;
    private Coroutine cdReadyCoroutine;

    // ═════════════════════════════════════════════════════
    //  INITIALIZATION
    // ═════════════════════════════════════════════════════

    private void Awake()
    {
        // Auto-detect references
        if (dash == null) dash = GetComponent<PlayerDash>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
        if (health == null) health = GetComponent<PlayerHealth>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (playerRenderer == null) playerRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        if (playerRenderer != null)
            originalColor = playerRenderer.material.color;

        CreateTrailRenderer();
        CreateDustParticles();
        CreateSparkleParticles();
        CreateHealParticles();
        CreateShockwave();
        CreateDashCooldownUI();

        // Subscribe to events
        if (dash != null)
        {
            dash.OnDashStarted += HandleDashStarted;
            dash.OnDashEnded += HandleDashEnded;
            dash.OnDashReady += HandleDashReady;
        }

        if (combat != null)
            combat.OnAttackPerformed += HandleAttack;

        if (health != null)
        {
            health.OnDamageTaken += HandleDamageTaken;
            health.OnHealActive += HandleHealActive;
            health.OnHPFull += HandleHPFull;
        }
    }

    private void OnDestroy()
    {
        if (dash != null)
        {
            dash.OnDashStarted -= HandleDashStarted;
            dash.OnDashEnded -= HandleDashEnded;
            dash.OnDashReady -= HandleDashReady;
        }

        if (combat != null)
            combat.OnAttackPerformed -= HandleAttack;

        if (health != null)
        {
            health.OnDamageTaken -= HandleDamageTaken;
            health.OnHealActive -= HandleHealActive;
            health.OnHPFull -= HandleHPFull;
        }
    }

    // ═════════════════════════════════════════════════════
    //  UPDATE — Continuous effects
    // ═════════════════════════════════════════════════════

    private void Update()
    {
        UpdateSparkles();
        UpdateDashCooldownUI();
        UpdateJitter();
    }

    // ═════════════════════════════════════════════════════
    //  DASH EFFECTS
    // ═════════════════════════════════════════════════════

    private void HandleDashStarted()
    {
        // Flash white
        FlashPlayer(dashFlashColor, dashFlashDuration);

        // Enable trail
        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }

        // Dust burst
        if (dustPS != null)
            dustPS.Emit(dustBurstCount);
    }

    private void HandleDashEnded()
    {
        // Disable trail after short delay to let it fade
        if (trail != null)
            StartCoroutine(DisableTrailDelayed(trailTime));
    }

    private void HandleDashReady()
    {
        // Flash the cooldown slider white then fade it out
        if (cdReadyCoroutine != null)
            StopCoroutine(cdReadyCoroutine);
        cdReadyCoroutine = StartCoroutine(DashReadyFlash());
    }

    private IEnumerator DisableTrailDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (trail != null)
            trail.emitting = false;
    }

    private IEnumerator DashReadyFlash()
    {
        if (cdFillImage != null)
            cdFillImage.color = cdReadyFlashColor;

        if (cdCanvasGroup != null)
        {
            cdCanvasGroup.alpha = 1f;
            float elapsed = 0f;
            float duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cdCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
            cdCanvasGroup.alpha = 0f;
        }

        cdReadyCoroutine = null;
    }

    // ═════════════════════════════════════════════════════
    //  ATTACK EFFECTS
    // ═════════════════════════════════════════════════════

    private void HandleAttack()
    {
        if (shockwaveCoroutine != null)
            StopCoroutine(shockwaveCoroutine);
        shockwaveCoroutine = StartCoroutine(ShockwaveExpand());
    }

    private IEnumerator ShockwaveExpand()
    {
        if (shockwaveGO == null || shockwaveSR == null) yield break;

        float hitRange = combat != null ? combat.hitRange : 3f;

        shockwaveGO.SetActive(true);
        shockwaveGO.transform.position = transform.position + Vector3.up * 0.05f;

        float elapsed = 0f;
        while (elapsed < shockwaveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shockwaveDuration;

            // Scale from 0 to hitRange * 2 (diameter)
            float diameter = Mathf.Lerp(0f, hitRange * 2f, t);
            shockwaveGO.transform.localScale = new Vector3(diameter, diameter, 1f);

            // Fade out
            Color c = shockwaveColor;
            c.a = Mathf.Lerp(shockwaveColor.a, 0f, t * t);
            shockwaveSR.color = c;

            yield return null;
        }

        shockwaveGO.SetActive(false);
        shockwaveCoroutine = null;
    }

    // ═════════════════════════════════════════════════════
    //  MOVEMENT SPARKLES
    // ═════════════════════════════════════════════════════

    private void UpdateSparkles()
    {
        if (sparklePS == null || movement == null) return;

        var emission = sparklePS.emission;
        // Emit particles only when moving
        bool isMoving = movement.LastMoveDirection.sqrMagnitude > 0.01f &&
                        GetComponent<Rigidbody>() != null &&
                        GetComponent<Rigidbody>().linearVelocity.sqrMagnitude > 0.5f;

        emission.rateOverTime = isMoving ? sparkleRatePerUnit * 4f : 0f;
    }

    // ═════════════════════════════════════════════════════
    //  HEAL / DAMAGE EFFECTS
    // ═════════════════════════════════════════════════════

    private void HandleHealActive()
    {
        if (healActivePS != null)
            healActivePS.Emit(healActiveBurstCount);
    }

    private void HandleHPFull()
    {
        FlashPlayer(hpFullFlashColor, hpFullFlashDuration);
    }

    private void HandleDamageTaken(int amount)
    {
        FlashPlayer(damageFlashColor, damageFlashDuration);
    }

    // ═════════════════════════════════════════════════════
    //  DASH COOLDOWN UI
    // ═════════════════════════════════════════════════════

    private void UpdateDashCooldownUI()
    {
        if (cdSlider == null || dash == null) return;

        float ratio = dash.GetCooldownRatio();

        if (ratio > 0f)
        {
            if (cdCanvasGroup != null && cdReadyCoroutine == null)
                cdCanvasGroup.alpha = 1f;
            cdSlider.value = 1f - ratio;
            if (cdFillImage != null && cdReadyCoroutine == null)
                cdFillImage.color = cdSliderColor;
        }
        else if (cdReadyCoroutine == null && cdCanvasGroup != null)
        {
            cdCanvasGroup.alpha = 0f;
        }
    }

    // ═════════════════════════════════════════════════════
    //  JITTER (during dash cooldown)
    // ═════════════════════════════════════════════════════

    private Vector3 jitterBaseLocalPos;
    private bool hasJitterBase;

    private void UpdateJitter()
    {
        if (playerRenderer == null || dash == null) return;

        if (!hasJitterBase)
        {
            jitterBaseLocalPos = playerRenderer.transform.localPosition;
            hasJitterBase = true;
        }

        if (dash.IsOnCooldown && !dash.IsDashing)
        {
            float offset = Mathf.Sin(Time.time * jitterFrequency) * jitterAmplitude;
            playerRenderer.transform.localPosition = jitterBaseLocalPos + new Vector3(offset, 0f, 0f);
        }
        else
        {
            playerRenderer.transform.localPosition = jitterBaseLocalPos;
        }
    }

    // ═════════════════════════════════════════════════════
    //  FLASH UTILITY
    // ═════════════════════════════════════════════════════

    private void FlashPlayer(Color color, float duration)
    {
        if (playerRenderer == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
    }

    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        playerRenderer.material.color = color;
        yield return new WaitForSeconds(duration);
        playerRenderer.material.color = originalColor;
        flashCoroutine = null;
    }

    // ═════════════════════════════════════════════════════
    //  PROCEDURAL CREATION — All effects auto-generated
    // ═════════════════════════════════════════════════════

    private void CreateTrailRenderer()
    {
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = trailWidth;
        trail.endWidth = 0f;
        trail.startColor = trailStartColor;
        trail.endColor = trailEndColor;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.emitting = false;
        trail.autodestruct = false;
        trail.minVertexDistance = 0.05f;
    }

    private void CreateDustParticles()
    {
        GameObject go = new GameObject("DashDustPS");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.down * 0.3f;

        dustPS = go.AddComponent<ParticleSystem>();
        var main = dustPS.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.startSize = 0.15f;
        main.startColor = dustColor;
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;

        var emission = dustPS.emission;
        emission.rateOverTime = 0;

        var shape = dustPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        dustPS.Stop();
    }

    private void CreateSparkleParticles()
    {
        GameObject go = new GameObject("MoveSparklePS");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.down * 0.2f;

        sparklePS = go.AddComponent<ParticleSystem>();
        var main = sparklePS.main;
        main.startLifetime = 0.4f;
        main.startSpeed = 0.5f;
        main.startSize = 0.08f;
        main.startColor = sparkleColor;
        main.gravityModifier = -0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;

        var emission = sparklePS.emission;
        emission.rateOverTime = 0;

        var shape = sparklePS.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.4f;

        var colorOverLifetime = sparklePS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(sparkleColor, 0f),
                new GradientColorKey(sparkleColor, 0.5f),
                new GradientColorKey(sparkleColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(1f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private void CreateHealParticles()
    {
        // ── Heal Active (burst, green/gold RPG style) ──
        GameObject goActive = new GameObject("HealActivePS");
        goActive.transform.SetParent(transform);
        goActive.transform.localPosition = Vector3.zero;

        healActivePS = goActive.AddComponent<ParticleSystem>();
        var mainA = healActivePS.main;
        mainA.startLifetime = 0.8f;
        mainA.startSpeed = 2f;
        mainA.startSize = 0.12f;
        mainA.startColor = healActiveColor;
        mainA.gravityModifier = -1f; // float upward
        mainA.simulationSpace = ParticleSystemSimulationSpace.World;
        mainA.maxParticles = 50;

        var emissionA = healActivePS.emission;
        emissionA.rateOverTime = 0;

        var shapeA = healActivePS.shape;
        shapeA.shapeType = ParticleSystemShapeType.Circle;
        shapeA.radius = 0.5f;

        var rendererA = goActive.GetComponent<ParticleSystemRenderer>();
        rendererA.material = new Material(Shader.Find("Particles/Standard Unlit"));
        rendererA.renderMode = ParticleSystemRenderMode.Billboard;

        healActivePS.Stop();

        // ── Heal Passive (subtle sparkles, constantly emitting during passive heal) ──
        GameObject goPassive = new GameObject("HealPassivePS");
        goPassive.transform.SetParent(transform);
        goPassive.transform.localPosition = Vector3.zero;

        healPassivePS = goPassive.AddComponent<ParticleSystem>();
        var mainP = healPassivePS.main;
        mainP.startLifetime = 1f;
        mainP.startSpeed = 0.8f;
        mainP.startSize = 0.06f;
        mainP.startColor = healPassiveColor;
        mainP.gravityModifier = -0.5f;
        mainP.simulationSpace = ParticleSystemSimulationSpace.World;
        mainP.maxParticles = 30;

        var emissionP = healPassivePS.emission;
        emissionP.rateOverTime = 0; // controlled in Update

        var shapeP = healPassivePS.shape;
        shapeP.shapeType = ParticleSystemShapeType.Circle;
        shapeP.radius = 0.3f;

        var rendererP = goPassive.GetComponent<ParticleSystemRenderer>();
        rendererP.material = new Material(Shader.Find("Particles/Standard Unlit"));
        rendererP.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private void CreateShockwave()
    {
        shockwaveGO = new GameObject("Shockwave");
        shockwaveGO.transform.SetParent(null); // world space
        shockwaveGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // flat on ground

        shockwaveSR = shockwaveGO.AddComponent<SpriteRenderer>();
        shockwaveSR.sprite = CreateCircleSprite(64);
        shockwaveSR.color = shockwaveColor;
        shockwaveSR.sortingOrder = 5;

        shockwaveGO.SetActive(false);
    }

    private void CreateDashCooldownUI()
    {
        // WorldSpace Canvas attached to player
        GameObject canvasGO = new GameObject("DashCooldownCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = new Vector3(0f, 0.1f, -1f);
        canvasGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        cdCanvas = canvasGO.AddComponent<Canvas>();
        cdCanvas.renderMode = RenderMode.WorldSpace;

        var scaler = canvasGO.AddComponent<CanvasScaler>();

        cdCanvasGroup = canvasGO.AddComponent<CanvasGroup>();
        cdCanvasGroup.alpha = 0f;
        cdCanvasGroup.interactable = false;
        cdCanvasGroup.blocksRaycasts = false;

        RectTransform crt = canvasGO.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(120, 16);
        crt.localScale = Vector3.one * 0.01f;

        // Slider
        GameObject sliderGO = new GameObject("CooldownSlider");
        sliderGO.transform.SetParent(canvasGO.transform, false);

        cdSlider = sliderGO.AddComponent<Slider>();
        RectTransform srt = sliderGO.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;
        cdSlider.direction = Slider.Direction.LeftToRight;
        cdSlider.minValue = 0f;
        cdSlider.maxValue = 1f;
        cdSlider.value = 0f;
        cdSlider.interactable = false;

        // Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.5f);
        bgImg.raycastTarget = false;
        RectTransform bgrt = bgGO.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fart = fillAreaGO.AddComponent<RectTransform>();
        fart.anchorMin = Vector2.zero;
        fart.anchorMax = Vector2.one;
        fart.offsetMin = Vector2.zero;
        fart.offsetMax = Vector2.zero;

        // Fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        cdFillImage = fillGO.AddComponent<Image>();
        cdFillImage.color = cdSliderColor;
        cdFillImage.raycastTarget = false;
        RectTransform frt = fillGO.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        cdSlider.fillRect = frt;
    }

    // ═════════════════════════════════════════════════════
    //  UTILITIES
    // ═════════════════════════════════════════════════════

    /// <summary>
    /// Génère un sprite cercle blanc procéduralement.
    /// </summary>
    private static Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float center = resolution / 2f;
        float radiusSq = center * center;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float distSq = dx * dx + dy * dy;

                if (distSq <= radiusSq)
                {
                    // Ring effect: only outer 20% is visible
                    float dist = Mathf.Sqrt(distSq);
                    float normalizedDist = dist / center;
                    float alpha = normalizedDist > 0.75f ? Mathf.InverseLerp(0.75f, 1f, normalizedDist) : 0f;
                    // Invert: outer ring is visible
                    alpha = normalizedDist > 0.8f ? 1f - Mathf.InverseLerp(0.8f, 1f, normalizedDist) : alpha;
                    alpha = Mathf.Clamp01(alpha * 3f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }
}
