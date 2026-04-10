using UnityEngine;


public class TimingJudge : MonoBehaviour
{
    public static TimingJudge Instance { get; private set; }

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

    public TimingResult Judge(float inputTime, float targetTime)
    {
        if (config == null)
        {
            Debug.LogWarning("TimingJudge: No GameConfig assigned!");
            return TimingResult.Miss;
        }

        float delta = inputTime - targetTime;
        float absDelta = Mathf.Abs(delta);

        if (absDelta <= config.perfectWindow)
        {
            return TimingResult.Perfect;
        }

        if (absDelta <= config.goodWindow)
        {
            return TimingResult.Good;
        }
        if (delta < 0 && absDelta <= config.tooSoonWindow)
        {
            return TimingResult.TooSoon;
        }
        if (delta > 0 && absDelta <= config.tooLateWindow)
        {
            return TimingResult.TooLate;
        }
        return TimingResult.Miss;
    }

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

    public static Color GetFeedbackColor(TimingResult result)
    {
        return result switch
        {
            TimingResult.Perfect => new Color(1f, 0.843f, 0f),
            TimingResult.Good => new Color(0f, 1f, 0.533f),
            TimingResult.TooSoon => new Color(1f, 0.267f, 0.267f),
            TimingResult.TooLate => new Color(1f, 0.533f, 0f),
            TimingResult.Miss => new Color(1f, 0f, 0f),
            _ => Color.white
        };
    }
}
