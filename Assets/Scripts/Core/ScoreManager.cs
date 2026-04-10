using UnityEngine;
using System;

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

    public int TotalScore => totalScore;

    public int Combo => combo;

    public float ScoreMultiplier => scoreMultiplier;

    public int BestCombo => bestCombo;

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

    public void RegisterHit(TimingResult result)
    {
        if (config == null) return;

        float timingBonus = result switch
        {
            TimingResult.Perfect => config.perfectScoreMultiplier,
            TimingResult.Good => config.goodScoreMultiplier,
            TimingResult.TooLate => config.tooLateScoreMultiplier,
            TimingResult.TooSoon => config.tooSoonScoreMultiplier,
            _ => 0f
        };

        int gained = Mathf.RoundToInt(config.baseScorePerKill * timingBonus * scoreMultiplier);
        totalScore += gained;

        combo++;
        if (combo > bestCombo) bestCombo = combo;

        float increment = result == TimingResult.Perfect
            ? config.scoreMultiplierIncrementPerfect
            : config.scoreMultiplierIncrementGood;
        scoreMultiplier = Mathf.Min(scoreMultiplier + increment, config.scoreMultiplierMax);

        if (SpeedMultiplier.Instance != null)
        {
            SpeedMultiplier.Instance.Recalculate(scoreMultiplier);
        }

        OnScoreChanged?.Invoke(totalScore);
        OnComboChanged?.Invoke(combo);
        OnMultiplierChanged?.Invoke(scoreMultiplier);
    }

    public void RegisterMiss()
    {
        if (config == null) return;

        combo = 0;
        scoreMultiplier = config.scoreMultiplierOnMiss;

        if (SpeedMultiplier.Instance != null)
        {
            SpeedMultiplier.Instance.ResetToBase();
        }

        OnComboChanged?.Invoke(combo);
        OnMultiplierChanged?.Invoke(scoreMultiplier);
    }
}

public enum TimingResult
{
    TooSoon,    // Trop tôt — pas de dégât, feedback négatif
    Perfect,    // Timing parfait — full damage + bonus score + speed up
    Good,       // Bon timing — full damage, score normal, léger speed up
    TooLate,    // Trop tard — demi-dégât, feedback
    Miss        // Raté — ennemi explose, dégâts au joueur, speed reset
}
