using UnityEngine;

/// <summary>
/// Évalue la précision du timing d'un input joueur par rapport au beat cible.
/// Les fenêtres de timing sont configurables via GameConfig.
/// </summary>
public class TimingJudge : MonoBehaviour
{
    public static TimingJudge Instance { get; private set; }

    [Header("=== Références ===")]
    [Tooltip("Configuration globale (contient les fenêtres de timing)")]
    public GameConfig config;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Évalue le timing d'une frappe.
    /// </summary>
    /// <param name="inputTime">Moment de la frappe (en secondes depuis le début de la musique)</param>
    /// <param name="targetTime">Moment idéal de la frappe (beat target en secondes)</param>
    /// <returns>Le résultat du timing</returns>
    public TimingResult Judge(float inputTime, float targetTime)
    {
        if (config == null)
        {
            Debug.LogWarning("TimingJudge: No GameConfig assigned!");
            return TimingResult.Miss;
        }

        float delta = inputTime - targetTime; // négatif = trop tôt, positif = trop tard
        float absDelta = Mathf.Abs(delta);

        // Perfect : dans ±perfectWindow
        if (absDelta <= config.perfectWindow)
        {
            return TimingResult.Perfect;
        }

        // Good : dans ±goodWindow
        if (absDelta <= config.goodWindow)
        {
            return TimingResult.Good;
        }

        // Trop tôt : avant le good window mais dans la zone tooSoon
        if (delta < 0 && absDelta <= config.tooSoonWindow)
        {
            return TimingResult.TooSoon;
        }

        // Trop tard : après le good window mais dans la zone tooLate
        if (delta > 0 && absDelta <= config.tooLateWindow)
        {
            return TimingResult.TooLate;
        }

        // Complètement raté
        return TimingResult.Miss;
    }

    /// <summary>
    /// Retourne un texte descriptif pour le résultat du timing.
    /// </summary>
    public static string GetFeedbackText(TimingResult result)
    {
        return result switch
        {
            TimingResult.Perfect => "PERFECT",
            TimingResult.Good => "GOOD",
            TimingResult.TooSoon => "TOO SOON",
            TimingResult.TooLate => "TOO LATE",
            TimingResult.Miss => "MISS",
            _ => ""
        };
    }

    /// <summary>
    /// Retourne la couleur de feedback pour le résultat du timing.
    /// </summary>
    public static Color GetFeedbackColor(TimingResult result)
    {
        return result switch
        {
            TimingResult.Perfect => new Color(1f, 0.843f, 0f),    // Or #FFD700
            TimingResult.Good => new Color(0f, 1f, 0.533f),        // Vert #00FF88
            TimingResult.TooSoon => new Color(1f, 0.267f, 0.267f), // Rouge #FF4444
            TimingResult.TooLate => new Color(1f, 0.533f, 0f),     // Orange #FF8800
            TimingResult.Miss => new Color(1f, 0f, 0f),            // Rouge vif
            _ => Color.white
        };
    }
}
