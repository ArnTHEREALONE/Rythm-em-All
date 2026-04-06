using UnityEngine;
using System;

/// <summary>
/// Système de score complet. Le multiplicateur de score drive la vitesse du jeu.
/// Chaque hit fait monter le combo → scoreMultiplier augmente → gameSpeed augmente.
/// Un miss remet tout à zéro.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("=== Références ===")]
    [Tooltip("Configuration globale du jeu")]
    public GameConfig config;

    [Header("=== État actuel (debug, lecture seule) ===")]
    [SerializeField] private int totalScore;
    [SerializeField] private int combo;
    [SerializeField] private float scoreMultiplier;
    [SerializeField] private int bestCombo;

    /// <summary>Score total accumulé.</summary>
    public int TotalScore => totalScore;

    /// <summary>Combo actuel (hits consécutifs sans miss).</summary>
    public int Combo => combo;

    /// <summary>Multiplicateur de score actuel.</summary>
    public float ScoreMultiplier => scoreMultiplier;

    /// <summary>Meilleur combo de la partie.</summary>
    public int BestCombo => bestCombo;

    // === Events pour l'UI ===
    public event Action<int> OnScoreChanged;
    public event Action<int> OnComboChanged;
    public event Action<float> OnMultiplierChanged;

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
        ResetAll();
    }

    /// <summary>
    /// Reset complet du score (début de partie).
    /// </summary>
    public void ResetAll()
    {
        totalScore = 0;
        combo = 0;
        bestCombo = 0;
        scoreMultiplier = config != null ? config.scoreMultiplierBase : 1f;

        OnScoreChanged?.Invoke(totalScore);
        OnComboChanged?.Invoke(combo);
        OnMultiplierChanged?.Invoke(scoreMultiplier);
    }

    /// <summary>
    /// Enregistre un hit réussi. Calcule le score, augmente le combo et le multiplicateur,
    /// puis recalcule la vitesse du jeu.
    /// </summary>
    public void RegisterHit(TimingResult result)
    {
        if (config == null) return;

        // 1. Calcul du bonus selon le timing
        float timingBonus = result switch
        {
            TimingResult.Perfect => config.perfectScoreMultiplier,
            TimingResult.Good => config.goodScoreMultiplier,
            TimingResult.TooLate => config.tooLateScoreMultiplier,
            TimingResult.TooSoon => config.tooSoonScoreMultiplier,
            _ => 0f
        };

        // 2. Calcul du score gagné : baseScore × timingBonus × scoreMultiplier
        int gained = Mathf.RoundToInt(config.baseScorePerKill * timingBonus * scoreMultiplier);
        totalScore += gained;

        // 3. Combo & multiplicateur de score
        combo++;
        if (combo > bestCombo) bestCombo = combo;

        float increment = result == TimingResult.Perfect
            ? config.scoreMultiplierIncrementPerfect
            : config.scoreMultiplierIncrementGood;
        scoreMultiplier = Mathf.Min(scoreMultiplier + increment, config.scoreMultiplierMax);

        // 4. Recalcule la vitesse globale du jeu
        if (SpeedMultiplier.Instance != null)
        {
            SpeedMultiplier.Instance.Recalculate(scoreMultiplier);
        }

        // 5. Notifie l'UI
        OnScoreChanged?.Invoke(totalScore);
        OnComboChanged?.Invoke(combo);
        OnMultiplierChanged?.Invoke(scoreMultiplier);
    }

    /// <summary>
    /// Enregistre un miss. Reset le combo, le multiplicateur et la vitesse.
    /// </summary>
    public void RegisterMiss()
    {
        if (config == null) return;

        combo = 0;
        scoreMultiplier = config.scoreMultiplierOnMiss;

        // Reset la vitesse du jeu
        if (SpeedMultiplier.Instance != null)
        {
            SpeedMultiplier.Instance.ResetToBase();
        }

        OnComboChanged?.Invoke(combo);
        OnMultiplierChanged?.Invoke(scoreMultiplier);
    }
}

/// <summary>
/// Résultat de l'évaluation du timing d'une frappe.
/// </summary>
public enum TimingResult
{
    TooSoon,    // Trop tôt — pas de dégât, feedback négatif
    Perfect,    // Timing parfait — full damage + bonus score + speed up
    Good,       // Bon timing — full damage, score normal, léger speed up
    TooLate,    // Trop tard — demi-dégât, feedback
    Miss        // Raté — ennemi explose, dégâts au joueur, speed reset
}
