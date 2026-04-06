using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Système de points de vie du joueur.
/// Inclut : dégâts, soin instantané on kill, soin progressif (passif).
/// Visualisé sur un UI Slider. Tous les paramètres sont dans PlayerConfig.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("=== Configuration ===")]
    public float maxHP = 100f;
    public float healOnKillAmount = 5f;
    public float passiveHealRate = 1f;
    public float passiveHealDelay = 3f;

    [Header("=== Références UI ===")]
    [Tooltip("Slider UI pour afficher les PV")]
    public Slider hpSlider;

    [Header("=== État (debug) ===")]
    [SerializeField] private float currentHP;
    [SerializeField] private float timeSinceLastDamage;
    [SerializeField] private bool isPassiveHealing;

    // === Events ===
    public event Action<float, float> OnHPChanged; // (current, max)
    public event Action OnDeath;

    /// <summary>PV actuels.</summary>
    public float CurrentHP => currentHP;

    /// <summary>Ratio de PV (0-1).</summary>
    public float HPRatio => maxHP > 0 ? currentHP / maxHP : 0f;

    /// <summary>Le joueur est-il mort ?</summary>
    public bool IsDead => currentHP <= 0f;

    private void Start()
    {
        currentHP = maxHP;
        timeSinceLastDamage = passiveHealDelay + 1f; // Commence avec le soin passif actif
        UpdateUI();
    }

    /// <summary>
    /// Initialise avec la config du joueur.
    /// </summary>
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

        // Timer pour le soin passif
        timeSinceLastDamage += Time.deltaTime;

        // Soin progressif après le délai
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

    /// <summary>
    /// Inflige des dégâts au joueur.
    /// </summary>
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

    /// <summary>
    /// Soin instantané lors d'un kill.
    /// </summary>
    public void HealOnKill()
    {
        if (IsDead) return;

        currentHP = Mathf.Min(currentHP + healOnKillAmount, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();
    }

    /// <summary>
    /// Soin progressif (appelé chaque frame après le délai).
    /// </summary>
    private void PassiveHeal()
    {
        currentHP = Mathf.Min(currentHP + passiveHealRate * Time.deltaTime, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();
    }

    /// <summary>
    /// Soigne complètement le joueur.
    /// </summary>
    public void FullHeal()
    {
        currentHP = maxHP;
        OnHPChanged?.Invoke(currentHP, maxHP);
        UpdateUI();
    }

    /// <summary>
    /// Met à jour le Slider UI.
    /// </summary>
    private void UpdateUI()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;

            // Changer la couleur selon les PV restants
            Image fill = hpSlider.fillRect?.GetComponent<Image>();
            if (fill != null)
            {
                float ratio = HPRatio;
                // Vert → Orange → Rouge
                if (ratio > 0.5f)
                    fill.color = Color.Lerp(new Color(1f, 0.5f, 0f), new Color(0f, 1f, 0.533f), (ratio - 0.5f) * 2f);
                else
                    fill.color = Color.Lerp(new Color(1f, 0.267f, 0.267f), new Color(1f, 0.5f, 0f), ratio * 2f);
            }
        }
    }
}
