using UnityEngine;
using System;

/// <summary>
/// Classe de base des ennemis. Cycle de vie :
/// Moving (clignotement sur les derniers N beats) → Vulnerable (jaune, continue de bouger) → Explode/Dead.
/// Le joueur peut tuer à tout moment, mais hors timing = penalty.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    public enum EnemyState
    {
        Moving,
        Vulnerable,
        Exploding,
        Dead
    }

    [Header("=== Configuration ===")]
    public EnemyData data;

    [Header("=== État (debug) ===")]
    [SerializeField] private EnemyState currentState = EnemyState.Moving;
    [SerializeField] private int currentHP;
    [SerializeField] private float targetBeatTime;
    [SerializeField] private float deathBeatTime;
    [SerializeField] private int spamClickCount;

    [Header("=== Visuel ===")]
    public GameObject vulnerableEffect;
    public Renderer mainRenderer;

    // === Events ===
    public event Action<EnemyBase, TimingResult> OnEnemyKilled;
    public event Action<EnemyBase> OnEnemyExploded;

    // === Propriétés ===
    public EnemyState CurrentState => currentState;
    public int CurrentHP => currentHP;
    public float TargetBeatTime => targetBeatTime;
    public EnemyInputType RequiredInput => data != null ? data.requiredInput : EnemyInputType.Any;

    // === Blink ===
    private Color originalColor;
    private float spamWindowStartTime;
    private int lastBlinkBeat = -1;
    private bool blinkVisible = true;

    private void Start()
    {
        if (mainRenderer == null)
            mainRenderer = GetComponentInChildren<Renderer>();

        if (mainRenderer != null)
            originalColor = mainRenderer.material.color;
    }

    /// <summary>
    /// Initialise l'ennemi lors du spawn.
    /// targetBeat = le beat auquel l'ennemi devient vulnérable.
    /// La durée de vie (quand il meurt naturellement) est calculée depuis le SO.
    /// </summary>
    public void Initialize(EnemyData enemyData, float targetBeat)
    {
        data = enemyData;
        targetBeatTime = targetBeat;
        currentHP = data.hitPoints;
        currentState = EnemyState.Moving;
        spamClickCount = 0;
        lastBlinkBeat = -1;
        blinkVisible = true;

        // Durée de vie : random entre min et max beats APRÈS le targetBeat
        float lifetime = data.lifetimeMinBeats;
        if (data.lifetimeMaxBeats > data.lifetimeMinBeats)
        {
            lifetime = UnityEngine.Random.Range(data.lifetimeMinBeats, data.lifetimeMaxBeats);
        }
        deathBeatTime = targetBeat + lifetime;

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
                // Clignoter sur les N derniers beats avant la vulnérabilité
                UpdateBlinking(currentBeat);

                if (currentBeat >= targetBeatTime)
                {
                    BecomeVulnerable();
                }
                break;

            case EnemyState.Vulnerable:
                // L'ennemi continue de bouger (EnemyMovement ne l'arrête plus)
                // Mort naturelle = quand on dépasse la durée de vie
                if (currentBeat >= deathBeatTime)
                {
                    Explode();
                }
                break;

            case EnemyState.Exploding:
                break;
        }
    }

    /// <summary>
    /// Clignotement rythmique : l'ennemi flash sur chaque beat pendant les N beats avant la vulnérabilité.
    /// </summary>
    private void UpdateBlinking(float currentBeat)
    {
        if (mainRenderer == null || data == null) return;

        float blinkStartBeat = targetBeatTime - data.blinkBeatsBeforeVulnerable;

        if (currentBeat < blinkStartBeat) return;

        // Déterminer si on est sur un nouveau beat
        int currentBeatInt = Mathf.FloorToInt(currentBeat);
        if (currentBeatInt > lastBlinkBeat)
        {
            lastBlinkBeat = currentBeatInt;
            blinkVisible = !blinkVisible;

            // Flash blanc/original
            mainRenderer.material.color = blinkVisible ? Color.white : originalColor;
        }
    }

    private void BecomeVulnerable()
    {
        currentState = EnemyState.Vulnerable;

        if (vulnerableEffect != null)
            vulnerableEffect.SetActive(true);

        // Couleur jaune pour l'état vulnérable
        if (mainRenderer != null)
            mainRenderer.material.color = Color.yellow;
    }

    /// <summary>
    /// Le joueur peut frapper à tout moment.
    /// - Si vulnérable : timing normal (Perfect/Good/TooLate)
    /// - Si Moving (trop tôt) : TooSoon → penalty
    /// - Hors timing complet : ForceKill → l'ennemi meurt mais score/vitesse reset
    /// </summary>
    public TimingResult TryHit(EnemyInputType inputType)
    {
        if (currentState == EnemyState.Dead || currentState == EnemyState.Exploding)
            return TimingResult.Miss;

        // L'ennemi peut toujours être frappé — on vérifie le timing

        // Check input type (sauf ForceKill qui ignore)
        if (data.requiredInput != EnemyInputType.Any &&
            data.requiredInput != EnemyInputType.Spam)
        {
            if (!IsCorrectInput(inputType))
                return TimingResult.Miss;
        }

        // Spam mode
        if (data.requiredInput == EnemyInputType.Spam && currentState == EnemyState.Vulnerable)
        {
            return HandleSpamHit();
        }

        // Calcul du timing
        float targetTime = BeatManager.Instance.BeatToSeconds(targetBeatTime);
        float hitTime = FMODAudioManager.Instance != null
            ? FMODAudioManager.Instance.GetTimelinePositionSeconds()
            : Time.time;

        TimingResult result;
        if (TimingJudge.Instance != null)
        {
            result = TimingJudge.Instance.Judge(hitTime, targetTime);
        }
        else
        {
            result = currentState == EnemyState.Vulnerable ? TimingResult.Good : TimingResult.TooSoon;
        }

        // Toujours infliger des dégâts (1 HP par hit)
        currentHP--;

        if (currentHP <= 0)
        {
            // Si le timing est mauvais (TooSoon ou Miss), on tue quand même
            // mais le résultat reste TooSoon/Miss pour que PlayerCombat applique la penalty
            if (result == TimingResult.TooSoon || result == TimingResult.Miss)
            {
                Die(TimingResult.TooSoon);
                return TimingResult.TooSoon;
            }
            else
            {
                Die(result);
            }
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
