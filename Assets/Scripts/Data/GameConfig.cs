using UnityEngine;

/// <summary>
/// Configuration globale du jeu. Tous les champs sont exposés dans l'Inspector Unity
/// pour pouvoir tweaker sans toucher au code.
/// Le multiplicateur de score drive la vitesse globale du jeu.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Scriptable Objects/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("=== SCORE ===")]
    [Tooltip("Score de base gagné par ennemi tué")]
    public int baseScorePerKill = 100;

    [Tooltip("Multiplicateur de score pour un timing Perfect (×2)")]
    public float perfectScoreMultiplier = 2f;

    [Tooltip("Multiplicateur de score pour un timing Good (×1)")]
    public float goodScoreMultiplier = 1f;

    [Tooltip("Multiplicateur de score pour un timing Too Late (×0.5)")]
    public float tooLateScoreMultiplier = 0.5f;

    [Tooltip("Multiplicateur de score pour un timing Too Soon (×0)")]
    public float tooSoonScoreMultiplier = 0f;

    [Header("=== COMBO & MULTIPLICATEUR DE SCORE ===")]
    [Tooltip("Valeur initiale du multiplicateur de score")]
    public float scoreMultiplierBase = 1f;

    [Tooltip("Incrément du multiplicateur par Perfect hit (+0.2)")]
    public float scoreMultiplierIncrementPerfect = 0.2f;

    [Tooltip("Incrément du multiplicateur par Good hit (+0.1)")]
    public float scoreMultiplierIncrementGood = 0.1f;

    [Tooltip("Valeur maximale du multiplicateur de score")]
    public float scoreMultiplierMax = 10f;

    [Tooltip("Valeur du multiplicateur après un miss (reset)")]
    public float scoreMultiplierOnMiss = 1f;

    [Header("=== VITESSE GLOBALE (liée au multiplicateur de score) ===")]
    [Tooltip("Vitesse de base du jeu")]
    public float baseGameSpeed = 1f;

    [Tooltip("Augmentation de vitesse par palier de multiplicateur de score")]
    public float speedPerScoreMultiplier = 0.05f;

    [Tooltip("Au-dessus de cette vitesse, diminishing returns sur l'augmentation")]
    public float speedSoftCap = 1.8f;

    [Tooltip("Vitesse maximale absolue (hard cap)")]
    public float speedHardCap = 2.5f;

    [Tooltip("Vitesse après un miss (reset)")]
    public float speedOnMiss = 1f;

    [Header("=== COOLDOWN SCALING ===")]
    [Tooltip("Les cooldowns sont divisés par (gameSpeed × ce facteur). 1 = scaling normal.")]
    public float cooldownSpeedFactor = 1f;

    [Header("=== FENÊTRES DE TIMING (secondes) ===")]
    [Tooltip("Fenêtre de timing pour un Perfect (±secondes)")]
    public float perfectWindow = 0.05f;

    [Tooltip("Fenêtre de timing pour un Good (±secondes)")]
    public float goodWindow = 0.12f;

    [Tooltip("Fenêtre 'trop tôt' avant le good window")]
    public float tooSoonWindow = 0.25f;

    [Tooltip("Fenêtre 'trop tard' après le good window")]
    public float tooLateWindow = 0.25f;

    /// <summary>
    /// Calcule la vitesse du jeu en fonction du score multiplier actuel.
    /// Applique softcap (diminishing returns) et hardcap.
    /// </summary>
    public float CalculateGameSpeed(float scoreMultiplier)
    {
        float rawSpeed = baseGameSpeed + (scoreMultiplier - 1f) * speedPerScoreMultiplier;

        // Softcap : diminishing returns au-dessus du seuil
        if (rawSpeed > speedSoftCap)
        {
            float excess = rawSpeed - speedSoftCap;
            float diminished = excess / (1f + excess);
            rawSpeed = speedSoftCap + diminished;
        }

        // Hardcap : jamais au-dessus
        return Mathf.Min(rawSpeed, speedHardCap);
    }
}
