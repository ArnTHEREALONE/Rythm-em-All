using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Santé du joueur. Gère HP, soin passif, soin on-kill, et flash du slider.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("=== Stats ===")]
    public float maxHP = 100f;
    public float healOnKillAmount = 5f;
    public float passiveHealRate = 1f;
    public float passiveHealDelay = 3f;

    [Header("=== UI ===")]
    public Slider hpSlider;

    [Header("=== Flash ===")]
    [Tooltip("Couleur du flash rouge quand le joueur perd du score (frappe hors timing)")]
    public Color flashColor = new Color(1f, 0f, 0f, 0.8f);
    [Tooltip("Durée du flash en secondes")]
    public float flashDuration = 0.3f;

    [Header("=== État (debug) ===")]
    [SerializeField] private float currentHP;
    [SerializeField] private float timeSinceLastDamage;
    [SerializeField] private bool isPassiveHealing;

    public event Action<float, float> OnHPChanged;
    public event Action OnDeath;

    public float CurrentHP => currentHP;
    public float HPRatio => maxHP > 0 ? currentHP / maxHP : 0f;
    public bool IsDead => currentHP <= 0f;

    private Image sliderFillImage;
    private Color normalFillColor;
    private Coroutine flashSliderCoroutine;

    private void Start()
    {
        currentHP = maxHP;
        timeSinceLastDamage = passiveHealDelay + 1f;

        // Cacher la ref du fill pour le flash
        if (hpSlider != null && hpSlider.fillRect != null)
        {
            sliderFillImage = hpSlider.fillRect.GetComponent<Image>();
        }

        UpdateUI();
    }

    public void Initialize(PlayerConfig config)
    {
        if (config != null)
        {
            maxHP = config.maxHP;
            healOnKillAmount = config.healOnKill;
            passiveHealRate = config.passiveHealRate;
            passiveHealDelay = config.passiveHealDelay;
        }

        currentHP = maxHP;
        UpdateUI();
    }

    private void Update()
    {
        if (IsDead) return;
        timeSinceLastDamage += Time.deltaTime;
        if (timeSinceLastDamage >= passiveHealDelay && currentHP < maxHP)
        {
            isPassiveHealing = true;
            PassiveHeal();
        }
        else
        {
            isPassiveHealing = false;
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        currentHP -= amount;
        timeSinceLastDamage = 0f;

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            OnDeath?.Invoke();
        }

        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();
    }

    public void HealOnKill()
    {
        if (IsDead) return;

        currentHP = Mathf.Min(currentHP + healOnKillAmount, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();
    }

    private void PassiveHeal()
    {
        currentHP = Mathf.Min(currentHP + passiveHealRate * Time.deltaTime, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();
    }

    public void FullHeal()
    {
        currentHP = maxHP;
        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();
    }

    /// <summary>
    /// Flash le slider HP en rouge (appelé quand le joueur frappe hors timing).
    /// </summary>
    public void FlashSliderRed()
    {
        if (sliderFillImage == null && hpSlider != null && hpSlider.fillRect != null)
            sliderFillImage = hpSlider.fillRect.GetComponent<Image>();

        if (sliderFillImage == null) return;

        if (flashSliderCoroutine != null)
            StopCoroutine(flashSliderCoroutine);

        flashSliderCoroutine = StartCoroutine(FlashSliderCoroutine());
    }

    private IEnumerator FlashSliderCoroutine()
    {
        if (sliderFillImage == null) yield break;

        Color originalColor = GetHealthColor();
        sliderFillImage.color = flashColor;

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            sliderFillImage.color = Color.Lerp(flashColor, GetHealthColor(), t);
            yield return null;
        }

        sliderFillImage.color = GetHealthColor();
        flashSliderCoroutine = null;
    }

    private Color GetHealthColor()
    {
        float ratio = HPRatio;
        if (ratio > 0.5f)
            return Color.Lerp(new Color(1f, 0.5f, 0f), new Color(0f, 1f, 0.533f), (ratio - 0.5f) * 2f);
        else
            return Color.Lerp(new Color(1f, 0.267f, 0.267f), new Color(1f, 0.5f, 0f), ratio * 2f);
    }

    private void UpdateUI()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;

            // Ne pas écraser la couleur si un flash est en cours
            if (flashSliderCoroutine == null && sliderFillImage != null)
            {
                sliderFillImage.color = GetHealthColor();
            }
        }
    }
}
