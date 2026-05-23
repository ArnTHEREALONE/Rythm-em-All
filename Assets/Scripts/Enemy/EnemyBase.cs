using UnityEngine;
using FMODUnity;
using System;






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

    
    public event Action<EnemyBase, TimingResult> OnEnemyKilled;
    public event Action<EnemyBase> OnEnemyExploded;

    
    public EnemyState CurrentState => currentState;
    public int CurrentHP => currentHP;
    public float TargetBeatTime => targetBeatTime;
    public EnemyInputType RequiredInput => data != null ? data.requiredInput : EnemyInputType.Any;

    
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

    
    
    
    
    
    public void Initialize(EnemyData enemyData, float spawnBeat)
    {
        data = enemyData;
        currentHP = data.hitPoints;
        currentState = EnemyState.Moving;
        spamClickCount = 0;
        lastBlinkBeat = -1;
        blinkVisible = true;

        // Le beat de spawn (passé par le SpawnManager = note.beatTime) est le moment du SPAWN.
        // Le timing de vulnérabilité est décalé de beatsBeforeVulnerable beats après.
        targetBeatTime = spawnBeat + data.beatsBeforeVulnerable;

        // L'auto-destruction se produit après la fenêtre de vulnérabilité.
        deathBeatTime = targetBeatTime + data.vulnerableWindowBeats;

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
                
                UpdateBlinking(currentBeat);

                float offset = 0f;
                if (SpeedMultiplier.Instance != null && SpeedMultiplier.Instance.config != null)
                    offset = SpeedMultiplier.Instance.config.vulnerabilityBeatOffset;

                if (currentBeat >= targetBeatTime + offset)
                {
                    BecomeVulnerable();
                }
                break;

            case EnemyState.Vulnerable:
                
                
                if (currentBeat >= deathBeatTime)
                {
                    Explode();
                }
                break;

            case EnemyState.Exploding:
                break;
        }
    }

    
    
    
    private void UpdateBlinking(float currentBeat)
    {
        if (mainRenderer == null || data == null) return;

        float blinkStartBeat = targetBeatTime - data.blinkBeatsBeforeVulnerable;

        if (currentBeat < blinkStartBeat) return;

        
        int currentBeatInt = Mathf.FloorToInt(currentBeat);
        if (currentBeatInt > lastBlinkBeat)
        {
            lastBlinkBeat = currentBeatInt;
            blinkVisible = !blinkVisible;

            
            mainRenderer.material.color = blinkVisible ? Color.white : originalColor;
        }
    }

    private void BecomeVulnerable()
    {
        currentState = EnemyState.Vulnerable;

        if (vulnerableEffect != null)
            vulnerableEffect.SetActive(true);

        
        if (mainRenderer != null)
            mainRenderer.material.color = Color.yellow;
    }

    
    
    
    
    
    
    public TimingResult TryHit(EnemyInputType inputType)
    {
        if (currentState == EnemyState.Dead || currentState == EnemyState.Exploding)
            return TimingResult.Miss;

        // Vérifier le type d'input requis
        if (data.requiredInput != EnemyInputType.Any &&
            data.requiredInput != EnemyInputType.Spam)
        {
            if (!IsCorrectInput(inputType))
                return TimingResult.Miss;
        }

        // Cas spam
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
            // Fallback : si vulnérable = Good, sinon Miss
            result = currentState == EnemyState.Vulnerable ? TimingResult.Good : TimingResult.Miss;
        }

        // Hors des fenêtres Perfect/Good = raté, inflige des dégâts au joueur.
        // TooSoon/TooLate/Miss sont tous traités comme un raté.
        if (result == TimingResult.TooSoon || result == TimingResult.TooLate || result == TimingResult.Miss)
        {
            return result; // Le PlayerCombat gère les dégâts pour TooSoon/TooLate
        }

        // Perfect ou Good : touche valide
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

        if (data != null && !data.deathByPlayerSFX.IsNull)
            RuntimeManager.PlayOneShot(data.deathByPlayerSFX);

        OnEnemyKilled?.Invoke(this, result);
        Destroy(gameObject, 0.1f);
    }

    private void Explode()
    {
        currentState = EnemyState.Exploding;

        if (vulnerableEffect != null)
            vulnerableEffect.SetActive(false);

        if (data != null && !data.deathNaturalSFX.IsNull)
            RuntimeManager.PlayOneShot(data.deathNaturalSFX);

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
