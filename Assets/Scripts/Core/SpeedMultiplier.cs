using UnityEngine;
using System;

public class SpeedMultiplier : MonoBehaviour
{
    public static SpeedMultiplier Instance { get; private set; }

    [Header("=== Références ===")]
    [Tooltip("Configuration globale du jeu")]
    public GameConfig config;

    [Header("=== Debug (lecture seule) ===")]
    [SerializeField] private float currentGameSpeed = 1f;
    [SerializeField] private float rawGameSpeed = 1f;

    public float CurrentGameSpeed => currentGameSpeed;

    public float RawGameSpeed => rawGameSpeed;

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

    public void Recalculate(float scoreMultiplier)
    {
        if (config == null) return;

        rawGameSpeed = config.baseGameSpeed + (scoreMultiplier - 1f) * config.speedPerScoreMultiplier;

        float capped = rawGameSpeed;
        if (capped > config.speedSoftCap)
        {
            float excess = capped - config.speedSoftCap;
            float diminished = excess / (1f + excess);
            capped = config.speedSoftCap + diminished;
        }
        currentGameSpeed = Mathf.Min(capped, config.speedHardCap);

        OnSpeedChanged?.Invoke(currentGameSpeed);
    }

    public void ResetToBase()
    {
        if (config == null) return;

        rawGameSpeed = config.speedOnMiss;
        currentGameSpeed = config.speedOnMiss;

        OnSpeedChanged?.Invoke(currentGameSpeed);
    }

    public float GetMusicPitch()
    {
        return currentGameSpeed;
    }

    public float GetScaledCooldown(float baseCooldown)
    {
        if (config == null) return baseCooldown;
        float factor = currentGameSpeed * config.cooldownSpeedFactor;
        return factor > 0f ? baseCooldown / factor : baseCooldown;
    }

    public float GetScaledSpeed(float baseSpeed)
    {
        return baseSpeed * currentGameSpeed;
    }
}
