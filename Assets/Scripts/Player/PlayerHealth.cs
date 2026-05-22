using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;




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
    [Tooltip("Couleur du flash rouge quand le joueur subit des dégâts ou frappe hors timing")]
    public Color flashColor = new Color(1f, 0f, 0f, 0.8f);
    [Tooltip("Durée du flash en secondes")]
    public float flashDuration = 0.3f;

    [Header("=== SFX (FMOD) ===")]
    public FMODUnity.EventReference healOnKillSFX;
    public FMODUnity.EventReference passiveHealSFX;
    public FMODUnity.EventReference healMaxSFX;
    public FMODUnity.EventReference damageSFX;

    [Header("=== État (debug) ===")]
    [SerializeField] private float currentHP;
    [SerializeField] private float timeSinceLastDamage;
    [SerializeField] private bool isPassiveHealing;

    public event Action<float, float> OnHPChanged;
    public event Action OnDeath;
    public event Action<int> OnDamageTaken;
    public event Action OnHealActive;
    public event Action OnHPFull;

    public float CurrentHP => currentHP;
    public float HPRatio => maxHP > 0 ? currentHP / maxHP : 0f;
    public bool IsDead => currentHP <= 0f;

    private Image sliderFillImage;
    private Coroutine flashSliderCoroutine;

    private void Start()
    {
        currentHP = maxHP;
        timeSinceLastDamage = passiveHealDelay + 1f;

        
        FindSliderFillImage();
        UpdateUI();
    }

    
    
    
    private void FindSliderFillImage()
    {
        if (hpSlider == null)
        {
            Debug.LogWarning("PlayerHealth: hpSlider est null ! Glisser le Slider HPBar dans l'Inspector.");
            return;
        }

        
        if (hpSlider.fillRect != null)
        {
            sliderFillImage = hpSlider.fillRect.GetComponent<Image>();
        }

        
        if (sliderFillImage == null)
        {
            Transform fillTransform = hpSlider.transform.Find("Fill Area/Fill");
            if (fillTransform != null)
            {
                sliderFillImage = fillTransform.GetComponent<Image>();
            }
        }

        
        if (sliderFillImage == null)
        {
            foreach (Transform child in hpSlider.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Fill")
                {
                    sliderFillImage = child.GetComponent<Image>();
                    if (sliderFillImage != null) break;
                }
            }
        }

        if (sliderFillImage == null)
        {
            Debug.LogWarning("PlayerHealth: Impossible de trouver l'Image Fill du slider HP. " +
                "Vérifier que le Slider a un Fill Area > Fill avec un composant Image.");
        }
        else
        {
            Debug.Log("PlayerHealth: Slider HP Fill trouvé correctement.");
        }
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
        FindSliderFillImage();
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

        Debug.Log($"PlayerHealth: Dégâts reçus = {amount}, HP = {currentHP}/{maxHP}");

        if (!damageSFX.IsNull && FMODAudioManager.Instance != null)
            FMODAudioManager.Instance.PlaySFX(damageSFX);

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            OnDeath?.Invoke();
        }

        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();

        OnDamageTaken?.Invoke(amount);
        FlashSliderRed();
    }

    public void HealOnKill()
    {
        if (IsDead) return;

        bool wasNotMax = currentHP < maxHP;
        currentHP = Mathf.Min(currentHP + healOnKillAmount, maxHP);
        
        if (!healOnKillSFX.IsNull && FMODAudioManager.Instance != null)
            FMODAudioManager.Instance.PlaySFX(healOnKillSFX);

        bool isNowFull = wasNotMax && currentHP >= maxHP;
        if (isNowFull && !healMaxSFX.IsNull && FMODAudioManager.Instance != null)
            FMODAudioManager.Instance.PlaySFX(healMaxSFX);

        OnHealActive?.Invoke();
        if (isNowFull) OnHPFull?.Invoke();

        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();
    }

    private float passiveHealSfxTimer = 0f;

    private void PassiveHeal()
    {
        bool wasNotMax = currentHP < maxHP;
        currentHP = Mathf.Min(currentHP + passiveHealRate * Time.deltaTime, maxHP);
        
        passiveHealSfxTimer -= Time.deltaTime;
        if (passiveHealSfxTimer <= 0f)
        {
            if (!passiveHealSFX.IsNull && FMODAudioManager.Instance != null)
                FMODAudioManager.Instance.PlaySFX(passiveHealSFX);
            passiveHealSfxTimer = 1f; // Play at most once per second
        }

        if (wasNotMax && currentHP >= maxHP && !healMaxSFX.IsNull && FMODAudioManager.Instance != null)
            FMODAudioManager.Instance.PlaySFX(healMaxSFX);

        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();
    }

    public void FullHeal()
    {
        bool wasNotMax = currentHP < maxHP;
        currentHP = maxHP;
        
        if (wasNotMax && !healMaxSFX.IsNull && FMODAudioManager.Instance != null)
            FMODAudioManager.Instance.PlaySFX(healMaxSFX);

        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();
    }

    
    
    
    public void FlashSliderRed()
    {
        if (sliderFillImage == null)
            FindSliderFillImage();

        if (sliderFillImage == null) return;

        if (flashSliderCoroutine != null)
            StopCoroutine(flashSliderCoroutine);

        flashSliderCoroutine = StartCoroutine(FlashSliderCoroutine());
    }

    private IEnumerator FlashSliderCoroutine()
    {
        if (sliderFillImage == null) yield break;

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

            
            if (flashSliderCoroutine == null && sliderFillImage != null)
            {
                sliderFillImage.color = GetHealthColor();
            }
        }
    }
}
