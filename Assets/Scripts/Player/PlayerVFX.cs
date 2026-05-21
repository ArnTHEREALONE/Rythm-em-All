using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;
using System.Collections;

/// <summary>
/// Centralise tous les effets visuels du joueur.
/// Utilise des références Inspector pour les particules (pas d'auto-génération).
/// Le joueur utilise un SpriteRenderer sur un enfant.
/// </summary>
public class PlayerVFX : MonoBehaviour
{
    [Header("=== Références (auto-détectées si vides) ===")]
    public PlayerDash dash;
    public PlayerCombat combat;
    public PlayerHealth health;
    public PlayerMovement movement;

    [Header("=== Sprite du joueur ===")]
    [Tooltip("Le SpriteRenderer du joueur (sur un enfant). Auto-détecté si vide.")]
    public SpriteRenderer playerSprite;

    // ─── DASH ────────────────────────────────────────────
    [Header("=== Dash — Flash ===")]
    public Color dashFlashColor = Color.white;
    public float dashFlashDuration = 0.06f;

    [Header("=== Dash — Trail ===")]
    public Color trailStartColor = new Color(1f, 1f, 1f, 0.8f);
    public Color trailEndColor = new Color(1f, 1f, 1f, 0f);
    public float trailTime = 0.2f;
    public float trailWidth = 0.3f;

    [Header("=== Dash — Particules (manuelles) ===")]
    [Tooltip("ParticleSystem one-shot pour la poussière de dash. Créer manuellement dans Unity et glisser ici.")]
    public ParticleSystem dashDustParticles;

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

    [Header("=== Attack — Flash ===")]
    [Tooltip("Le sprite s'assombrit brièvement pendant l'attaque")]
    public Color attackDarkenColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public float attackFlashDuration = 0.08f;

    [Header("=== Attack — Range Circle ===")]
    [Tooltip("Afficher un cercle de portée d'attaque permanent")]
    public bool showAttackRange = true;
    public Color attackRangeColor = new Color(1f, 0.3f, 0.3f, 0.15f);
    [Tooltip("Épaisseur du cercle")]
    public float attackRangeLineWidth = 0.05f;

    // ─── MOVEMENT ────────────────────────────────────────
    [Header("=== Movement — Particules (manuelles) ===")]
    [Tooltip("ParticleSystem en loop pour les paillettes de déplacement. Créer manuellement dans Unity et glisser ici.")]
    public ParticleSystem moveSparkleParticles;

    [Header("=== Movement — Sprite Deformation ===")]
    [Tooltip("Échelle max en Z (avant = allongement) à vitesse maximale")]
    public float maxStretchZ = 1.4f;
    [Tooltip("Échelle min en X (latéral = tassement) à vitesse maximale")]
    public float minSquashX = 0.7f;
    [Tooltip("Vitesse au-delà de laquelle la déformation est maximale")]
    public float deformMaxSpeed = 15f;
    [Tooltip("Vitesse de rotation du sprite vers la direction de déplacement")]
    public float spriteRotationSpeed = 15f;

    // ─── HEAL ────────────────────────────────────────────
    [Header("=== Heal — VFX Graph (manuels) ===")]
    [Tooltip("VFX Graph one-shot pour le heal actif (on kill). Créer dans Unity et glisser ici.")]
    public VisualEffect healActiveVFX;
    [Tooltip("VFX Graph en loop pour le heal passif. Créer dans Unity et glisser ici.")]
    public VisualEffect healPassiveVFX;

    [Header("=== HP Full — Flash ===")]
    public Color hpFullFlashColor = new Color(0.2f, 1f, 0.4f, 1f);
    public float hpFullFlashDuration = 0.15f;

    [Header("=== Damage — Flash ===")]
    public Color damageFlashColor = new Color(1f, 0.15f, 0.15f, 1f);
    public float damageFlashDuration = 0.12f;

    // ─── Internals ───────────────────────────────────────
    private Color originalSpriteColor;
    private TrailRenderer trail;
    private GameObject shockwaveGO;
    private SpriteRenderer shockwaveSR;
    private LineRenderer attackRangeLR;

    // Dash cooldown UI
    private Canvas cdCanvas;
    private Slider cdSlider;
    private Image cdFillImage;
    private CanvasGroup cdCanvasGroup;

    private Coroutine flashCoroutine;
    private Coroutine shockwaveCoroutine;
    private Coroutine cdReadyCoroutine;

    // Sprite deformation
    private Transform spriteTransform;
    private Vector3 spriteBaseScale;
    private Quaternion spriteBaseRotation;
    private Rigidbody rb;

    // ═════════════════════════════════════════════════════
    //  INITIALIZATION
    // ═════════════════════════════════════════════════════

    private void Awake()
    {
        if (dash == null) dash = GetComponent<PlayerDash>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
        if (health == null) health = GetComponent<PlayerHealth>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (playerSprite == null) playerSprite = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (playerSprite != null)
        {
            originalSpriteColor = playerSprite.color;
            spriteTransform = playerSprite.transform;
            spriteBaseScale = spriteTransform.localScale;
            spriteBaseRotation = spriteTransform.localRotation;
        }

        CreateTrailRenderer();
        CreateShockwave();
        CreateDashCooldownUI();
        if (showAttackRange) CreateAttackRangeCircle();

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

        // Disable passive heal VFX at start
        if (healPassiveVFX != null)
            healPassiveVFX.Stop();
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

        if (shockwaveGO != null)
            Destroy(shockwaveGO);
    }

    // ═════════════════════════════════════════════════════
    //  UPDATE
    // ═════════════════════════════════════════════════════

    private void Update()
    {
        UpdateMoveParticles();
        UpdateDashCooldownUI();
        UpdateJitter();
        UpdateSpriteDeformation();
        UpdateAttackRangeCircle();
        UpdatePassiveHealVFX();
    }

    // ═════════════════════════════════════════════════════
    //  DASH EFFECTS
    // ═════════════════════════════════════════════════════

    private void HandleDashStarted()
    {
        FlashSprite(dashFlashColor, dashFlashDuration);

        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }

        // One-shot dust particles (manually created)
        if (dashDustParticles != null)
            dashDustParticles.Play();
    }

    private void HandleDashEnded()
    {
        if (trail != null)
            StartCoroutine(DisableTrailDelayed(trailTime));
    }

    private void HandleDashReady()
    {
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
        // Darken sprite briefly (not same color as shockwave)
        FlashSprite(attackDarkenColor, attackFlashDuration);

        // Shockwave
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

            float diameter = Mathf.Lerp(0f, hitRange * 2f, t);
            shockwaveGO.transform.localScale = new Vector3(diameter, diameter, 1f);

            Color c = shockwaveColor;
            c.a = Mathf.Lerp(shockwaveColor.a, 0f, t * t);
            shockwaveSR.color = c;

            yield return null;
        }

        shockwaveGO.SetActive(false);
        shockwaveCoroutine = null;
    }

    // ═════════════════════════════════════════════════════
    //  MOVEMENT SPARKLES (manual ParticleSystem)
    // ═════════════════════════════════════════════════════

    private void UpdateMoveParticles()
    {
        if (moveSparkleParticles == null || rb == null) return;

        var emission = moveSparkleParticles.emission;
        bool isMoving = rb.linearVelocity.sqrMagnitude > 0.5f;

        if (isMoving && !moveSparkleParticles.isPlaying)
            moveSparkleParticles.Play();
        else if (!isMoving && moveSparkleParticles.isPlaying)
            moveSparkleParticles.Stop();
    }

    // ═════════════════════════════════════════════════════
    //  SPRITE DEFORMATION & ROTATION
    // ═════════════════════════════════════════════════════

    private void UpdateSpriteDeformation()
    {
        if (spriteTransform == null || rb == null) return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        // ── Rotation toward movement direction (cosmetic only) ──
        if (speed > 0.3f)
        {
            // Calculate target rotation looking in movement direction
            // Since camera is top-down at Y=15, we rotate around Y axis
            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            spriteTransform.rotation = Quaternion.Slerp(
                spriteTransform.rotation,
                targetRot,
                Time.deltaTime * spriteRotationSpeed
            );
        }

        // ── Scale deformation based on speed ──
        float t = Mathf.Clamp01(speed / deformMaxSpeed);

        // Stretch in Z (forward), squash in X (lateral), relative to sprite
        float scaleZ = Mathf.Lerp(spriteBaseScale.z, spriteBaseScale.z * maxStretchZ, t);
        float scaleX = Mathf.Lerp(spriteBaseScale.x, spriteBaseScale.x * minSquashX, t);
        float scaleY = spriteBaseScale.y; // unchanged

        spriteTransform.localScale = new Vector3(scaleX, scaleY, scaleZ);
    }

    // ═════════════════════════════════════════════════════
    //  HEAL / DAMAGE EFFECTS
    // ═════════════════════════════════════════════════════

    private void HandleHealActive()
    {
        // One-shot VFX Graph
        if (healActiveVFX != null)
        {
            healActiveVFX.Play();
        }
    }

    private void UpdatePassiveHealVFX()
    {
        if (healPassiveVFX == null || health == null) return;

        bool isPassiveHealing = !health.IsDead &&
                                health.CurrentHP < health.maxHP &&
                                health.CurrentHP > 0f;

        // We check a rough proxy: the health script heals passively
        // after passiveHealDelay seconds without damage.
        // We can check if HP is increasing by comparing states,
        // but simpler: just check if HP < max and time since damage > delay
        // Unfortunately we don't have direct access to timeSinceLastDamage.
        // So we check if the object reports passive healing via the existing system.

        // Simple approach: if HP < max, assume passive healing could happen
        // The VFX graph should be subtle enough that it's fine
        if (isPassiveHealing)
        {
            if (!healPassiveVFX.HasAnySystemAwake())
                healPassiveVFX.Play();
        }
        else
        {
            if (healPassiveVFX.HasAnySystemAwake())
                healPassiveVFX.Stop();
        }
    }

    private void HandleHPFull()
    {
        FlashSprite(hpFullFlashColor, hpFullFlashDuration);
    }

    private void HandleDamageTaken(int amount)
    {
        FlashSprite(damageFlashColor, damageFlashDuration);
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
    //  JITTER
    // ═════════════════════════════════════════════════════

    private Vector3 jitterBaseLocalPos;
    private bool hasJitterBase;

    private void UpdateJitter()
    {
        if (spriteTransform == null || dash == null) return;

        if (!hasJitterBase)
        {
            jitterBaseLocalPos = spriteTransform.localPosition;
            hasJitterBase = true;
        }

        if (dash.IsOnCooldown && !dash.IsDashing)
        {
            float offset = Mathf.Sin(Time.time * jitterFrequency) * jitterAmplitude;
            spriteTransform.localPosition = jitterBaseLocalPos + new Vector3(offset, 0f, 0f);
        }
        else
        {
            spriteTransform.localPosition = jitterBaseLocalPos;
        }
    }

    // ═════════════════════════════════════════════════════
    //  FLASH (SpriteRenderer)
    // ═════════════════════════════════════════════════════

    private void FlashSprite(Color color, float duration)
    {
        if (playerSprite == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
    }

    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        playerSprite.color = color;
        yield return new WaitForSeconds(duration);
        playerSprite.color = originalSpriteColor;
        flashCoroutine = null;
    }

    // ═════════════════════════════════════════════════════
    //  ATTACK RANGE CIRCLE
    // ═════════════════════════════════════════════════════

    private void CreateAttackRangeCircle()
    {
        GameObject rangeGO = new GameObject("AttackRangeVFX");
        rangeGO.transform.SetParent(transform);
        rangeGO.transform.localPosition = Vector3.up * 0.02f;

        attackRangeLR = rangeGO.AddComponent<LineRenderer>();
        attackRangeLR.useWorldSpace = false;
        attackRangeLR.loop = true;
        attackRangeLR.startWidth = attackRangeLineWidth;
        attackRangeLR.endWidth = attackRangeLineWidth;
        attackRangeLR.material = new Material(Shader.Find("Sprites/Default"));
        attackRangeLR.startColor = attackRangeColor;
        attackRangeLR.endColor = attackRangeColor;
        attackRangeLR.sortingOrder = 1;

        int segments = 64;
        attackRangeLR.positionCount = segments;
    }

    private void UpdateAttackRangeCircle()
    {
        if (attackRangeLR == null || combat == null) return;

        float radius = combat.hitRange;
        int segments = attackRangeLR.positionCount;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            attackRangeLR.SetPosition(i, new Vector3(x, 0f, z));
        }
    }

    // ═════════════════════════════════════════════════════
    //  PROCEDURAL CREATION
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

    private void CreateShockwave()
    {
        shockwaveGO = new GameObject("Shockwave");
        shockwaveGO.transform.SetParent(null);
        shockwaveGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        shockwaveSR = shockwaveGO.AddComponent<SpriteRenderer>();
        shockwaveSR.sprite = CreateCircleSprite(64);
        shockwaveSR.color = shockwaveColor;
        shockwaveSR.sortingOrder = 5;

        shockwaveGO.SetActive(false);
    }

    private void CreateDashCooldownUI()
    {
        GameObject canvasGO = new GameObject("DashCooldownCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = new Vector3(0f, 0.1f, -1f);
        canvasGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        cdCanvas = canvasGO.AddComponent<Canvas>();
        cdCanvas.renderMode = RenderMode.WorldSpace;

        canvasGO.AddComponent<CanvasScaler>();

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
                    float dist = Mathf.Sqrt(distSq);
                    float normalizedDist = dist / center;
                    float alpha = normalizedDist > 0.75f ? Mathf.InverseLerp(0.75f, 1f, normalizedDist) : 0f;
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
