using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerHealth : MonoBehaviour
{
    public float maxHP = 100f;
    public float healOnKillAmount = 5f;
    public float passiveHealRate = 1f;
    public float passiveHealDelay = 3f;

    public Slider hpSlider;

    [SerializeField] private float currentHP;
    [SerializeField] private float timeSinceLastDamage;
    [SerializeField] private bool isPassiveHealing;

    public event Action<float, float> OnHPChanged;
    public event Action OnDeath;

    public float CurrentHP => currentHP;

    public float HPRatio => maxHP > 0 ? currentHP / maxHP : 0f;

    public bool IsDead => currentHP <= 0f;

    private void Start()
    {
        currentHP = maxHP;
        timeSinceLastDamage = passiveHealDelay + 1f;
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

    private void UpdateUI()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;

            Image fill = hpSlider.fillRect?.GetComponent<Image>();
            if (fill != null)
            {
                float ratio = HPRatio;
                if (ratio > 0.5f)
                    fill.color = Color.Lerp(new Color(1f, 0.5f, 0f), new Color(0f, 1f, 0.533f), (ratio - 0.5f) * 2f);
                else
                    fill.color = Color.Lerp(new Color(1f, 0.267f, 0.267f), new Color(1f, 0.5f, 0f), ratio * 2f);
            }
        }
    }
}
