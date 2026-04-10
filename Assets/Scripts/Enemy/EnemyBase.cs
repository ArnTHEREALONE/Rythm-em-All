using UnityEngine;
using System;

public class EnemyBase : MonoBehaviour
{
    public enum EnemyState
    {
        Spawning,
        Moving,
        Vulnerable,
        Exploding,
        Dead
    }

    public EnemyData data;

    [SerializeField] private EnemyState currentState = EnemyState.Spawning;
    [SerializeField] private int currentHP;
    [SerializeField] private float targetBeatTime;
    [SerializeField] private float vulnerableStartTime;
    [SerializeField] private int spamClickCount;

    public float vulnerableDuration = 0.5f;

    public GameObject vulnerableEffect;

    public Renderer mainRenderer;

    public event Action<EnemyBase, TimingResult> OnEnemyKilled;
    public event Action<EnemyBase> OnEnemyExploded;

    public EnemyState CurrentState => currentState;
    public int CurrentHP => currentHP;
    public float TargetBeatTime => targetBeatTime;
    public EnemyInputType RequiredInput => data != null ? data.requiredInput : EnemyInputType.Any;

    private Color originalColor;
    private float spamWindowStartTime;

    private void Start()
    {
        if (mainRenderer == null)
            mainRenderer = GetComponentInChildren<Renderer>();

        if (mainRenderer != null)
            originalColor = mainRenderer.material.color;
    }

    public void Initialize(EnemyData enemyData, float targetBeat)
    {
        data = enemyData;
        targetBeatTime = targetBeat;
        currentHP = data.hitPoints;
        currentState = EnemyState.Moving;
        spamClickCount = 0;

        if (vulnerableEffect != null)
            vulnerableEffect.SetActive(false);
    }

    private void Update()
    {
        if (BeatManager.Instance == null) return;

        float currentBeat = BeatManager.Instance.CurrentBeat;

        switch (currentState)
        {
            case EnemyState.Moving:
                if (currentBeat >= targetBeatTime)
                {
                    BecomeVulnerable();
                }
                break;

            case EnemyState.Vulnerable:
                float elapsed = Time.time - vulnerableStartTime;
                if (elapsed >= vulnerableDuration)
                {
                    Explode();
                }
                break;

            case EnemyState.Exploding:
                break;
        }
    }

    private void BecomeVulnerable()
    {
        currentState = EnemyState.Vulnerable;
        vulnerableStartTime = Time.time;

        if (vulnerableEffect != null)
            vulnerableEffect.SetActive(true);

        if (mainRenderer != null)
            mainRenderer.material.color = Color.white;
    }

    public TimingResult TryHit(EnemyInputType inputType)
    {
        if (currentState == EnemyState.Moving)
        {
            float targetTimeInSeconds = BeatManager.Instance.BeatToSeconds(targetBeatTime);
            float currentTime = AudioManager.Instance != null ? AudioManager.Instance.MusicTime : Time.time;

            TimingResult earlyResult = TimingJudge.Instance != null
                ? TimingJudge.Instance.Judge(currentTime, targetTimeInSeconds)
                : TimingResult.Miss;

            if (earlyResult == TimingResult.TooSoon)
                return TimingResult.TooSoon;

            return TimingResult.Miss;
        }

        if (currentState != EnemyState.Vulnerable)
            return TimingResult.Miss;

        if (!IsCorrectInput(inputType))
            return TimingResult.Miss;

        if (data.requiredInput == EnemyInputType.Spam)
        {
            return HandleSpamHit();
        }

        float targetTime = BeatManager.Instance.BeatToSeconds(targetBeatTime);
        float hitTime = AudioManager.Instance != null ? AudioManager.Instance.MusicTime : Time.time;

        TimingResult result = TimingJudge.Instance != null
            ? TimingJudge.Instance.Judge(hitTime, targetTime)
            : TimingResult.Good;

        currentHP--;
        if (currentHP <= 0)
        {
            Die(result);
        }

        return result;
    }

    private bool IsCorrectInput(EnemyInputType input)
    {
        return data.requiredInput switch
        {
            EnemyInputType.Any => true,
            EnemyInputType.LeftOnly => input == EnemyInputType.LeftOnly,
            EnemyInputType.RightOnly => input == EnemyInputType.RightOnly,
            EnemyInputType.Both => input == EnemyInputType.Both,
            EnemyInputType.Spam => input == EnemyInputType.LeftOnly || input == EnemyInputType.RightOnly || input == EnemyInputType.Any,
            _ => false
        };
    }

    private TimingResult HandleSpamHit()
    {
        if (spamClickCount == 0)
        {
            spamWindowStartTime = Time.time;
        }

        spamClickCount++;

        if (spamClickCount >= data.spamClicksRequired)
        {
            float elapsed = Time.time - spamWindowStartTime;
            if (elapsed <= data.spamWindowDuration)
            {
                currentHP = 0;
                Die(TimingResult.Good);
                return TimingResult.Good;
            }
            else
            {
                spamClickCount = 0;
                return TimingResult.TooLate;
            }
        }

        return TimingResult.Good;
    }

    private void Die(TimingResult result)
    {
        currentState = EnemyState.Dead;

        if (vulnerableEffect != null)
            vulnerableEffect.SetActive(false);

        OnEnemyKilled?.Invoke(this, result);

        Destroy(gameObject, 0.1f);
    }

    private void Explode()
    {
        currentState = EnemyState.Exploding;

        if (vulnerableEffect != null)
            vulnerableEffect.SetActive(false);

        OnEnemyExploded?.Invoke(this);

        Destroy(gameObject, 0.3f);
    }

    public float GetTargetTimeInSeconds()
    {
        if (BeatManager.Instance != null)
            return BeatManager.Instance.BeatToSeconds(targetBeatTime);
        return 0f;
    }
}
