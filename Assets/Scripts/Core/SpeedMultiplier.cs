using UnityEngine;
using System;

/// <summary>
/// Multiplicateur de vitesse global du jeu. Singleton.
/// Piloté par le ScoreManager : quand le score multiplier change, la vitesse du jeu change.
/// Affecte : musique (pitch), joueur, ennemis, cooldowns.
/// </summary>
public class SpeedMultiplier : MonoBehaviour
{
    public static SpeedMultiplier Instance { get; private set; }

    [Header("=== Références ===")]
    [Tooltip("Configuration globale du jeu")]
    public GameConfig config;

    [Header("=== Debug (lecture seule) ===")]
    [SerializeField] private float currentGameSpeed = 1f;
    [SerializeField] private float rawGameSpeed = 1f;

    /// <summary>
    /// Vitesse actuelle du jeu (avec caps appliqués).
    /// </summary>
    public float CurrentGameSpeed => currentGameSpeed;

    /// <summary>
    /// Vitesse avant application des caps (pour debug).
    /// </summary>
    public float RawGameSpeed => rawGameSpeed;

    /// <summary>
    /// Événement déclenché quand la vitesse change.
    /// </summary>
    public event Action<float> OnSpeedChanged;

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
        if (config != null)
        {
            currentGameSpeed = config.baseGameSpeed;
            rawGameSpeed = config.baseGameSpeed;
        }
    }

    /// <summary>
    /// Recalcule la vitesse du jeu en fonction du score multiplier actuel.
    /// Appelé par ScoreManager quand le multiplicateur change.
    /// </summary>
    public void Recalculate(float scoreMultiplier)
    {
        if (config == null) return;

        rawGameSpeed = config.baseGameSpeed + (scoreMultiplier - 1f) * config.speedPerScoreMultiplier;

        // Softcap : diminishing returns au-dessus du seuil
        float capped = rawGameSpeed;
        if (capped > config.speedSoftCap)
        {
            float excess = capped - config.speedSoftCap;
            float diminished = excess / (1f + excess);
            capped = config.speedSoftCap + diminished;
        }

        // Hardcap
        currentGameSpeed = Mathf.Min(capped, config.speedHardCap);

        OnSpeedChanged?.Invoke(currentGameSpeed);
    }

    /// <summary>
    /// Reset la vitesse à la valeur de base (appelé on miss).
    /// </summary>
    public void ResetToBase()
    {
        if (config == null) return;

        rawGameSpeed = config.speedOnMiss;
        currentGameSpeed = config.speedOnMiss;

        OnSpeedChanged?.Invoke(currentGameSpeed);
    }

    /// <summary>
    /// Retourne le pitch à appliquer à l'AudioSource de la musique.
    /// </summary>
    public float GetMusicPitch()
    {
        return currentGameSpeed;
    }

    /// <summary>
    /// Retourne un cooldown scalé par la vitesse du jeu.
    /// Plus le jeu est rapide, plus le cooldown est court.
    /// </summary>
    public float GetScaledCooldown(float baseCooldown)
    {
        if (config == null) return baseCooldown;
        float factor = currentGameSpeed * config.cooldownSpeedFactor;
        return factor > 0f ? baseCooldown / factor : baseCooldown;
    }

    /// <summary>
    /// Retourne une vitesse scalée par la vitesse du jeu.
    /// </summary>
    public float GetScaledSpeed(float baseSpeed)
    {
        return baseSpeed * currentGameSpeed;
    }
}
