using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Scriptable Objects/GameConfig")]
public class GameConfig : ScriptableObject
{

    public int baseScorePerKill = 100;

    public float perfectScoreMultiplier = 2f;

    public float goodScoreMultiplier = 1f;

    public float tooLateScoreMultiplier = 0.5f;

    public float tooSoonScoreMultiplier = 0f;

    public float scoreMultiplierBase = 1f;

    public float scoreMultiplierIncrementPerfect = 0.2f;

    public float scoreMultiplierIncrementGood = 0.1f;

    public float scoreMultiplierMax = 10f;

    public float scoreMultiplierOnMiss = 1f;

    public float baseGameSpeed = 1f;

    public float speedPerScoreMultiplier = 0.05f;

    public float speedSoftCap = 1.8f;

    public float speedHardCap = 2.5f;

    public float speedOnMiss = 1f;

    public float cooldownSpeedFactor = 1f;

    public float perfectWindow = 0.05f;

    public float goodWindow = 0.12f;

    public float tooSoonWindow = 0.25f;

    public float tooLateWindow = 0.25f;

    [Header("=== Timing Offset ===")]
    [Tooltip("Décale le moment où l'ennemi devient vulnérable par rapport à son beat.\nValeur négative = devient vulnérable plus tôt.\nValeur positive = devient vulnérable plus tard.\nPar défaut : 0.")]
    public float vulnerabilityBeatOffset = 0f;

    public float CalculateGameSpeed(float scoreMultiplier)
    {
        float rawSpeed = baseGameSpeed + (scoreMultiplier - 1f) * speedPerScoreMultiplier;

        if (rawSpeed > speedSoftCap)
        {
            float excess = rawSpeed - speedSoftCap;
            float diminished = excess / (1f + excess);
            rawSpeed = speedSoftCap + diminished;
        }

        return Mathf.Min(rawSpeed, speedHardCap);
    }
}
