using UnityEngine;
using System;

/// <summary>
/// Classe de base des ennemis. Gère le cycle de vie complet :
/// Spawn → Moving → Vulnerable → (Hit/Exploding) → Dead.
/// L'ennemi se déplace en ligne droite et devient vulnérable à un moment précis (beat target).
/// </summary>
public class EnemyBase : MonoBehaviour
{
    /// <summary>
    /// États possibles de l'ennemi.
    /// </summary>
    public enum EnemyState
    {
        Spawning,    // Vient d'apparaître, animation d'entrée
        Moving,      // Se déplace vers le centre
        Vulnerable,  // Peut être frappé par le joueur
        Exploding,   // N'a pas été frappé à temps, explose
        Dead         // Mort, sera détruit
    }

    [Header("=== Configuration ===")]
    [Tooltip("Données de ce type d'ennemi")]
    public EnemyData data;

    [Header("=== État (debug) ===")]
    [SerializeField] private EnemyState currentState = EnemyState.Spawning;
    [SerializeField] private int currentHP;
    [SerializeField] private float targetBeatTime;     // beat auquel il devient vulnérable
    [SerializeField] private float vulnerableStartTime; // temps réel où il est devenu vulnérable
    [SerializeField] private int spamClickCount;        // compteur pour le mode Spam

    [Header("=== Timing ===")]
    [Tooltip("Durée de la fenêtre de vulnérabilité en secondes")]
    public float vulnerableDuration = 0.5f;

    [Header("=== Visuel ===")]
    [Tooltip("Effet visuel quand l'ennemi est vulnérable (à activer par script)")]
    public GameObject vulnerableEffect;

    [Tooltip("Renderer principal de l'ennemi (pour changer la couleur)")]
    public Renderer mainRenderer;

    // === Events ===
    public event Action<EnemyBase, TimingResult> OnEnemyKilled;
    public event Action<EnemyBase> OnEnemyExploded;

    // === Propriétés ===
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

    /// <summary>
    /// Initialise l'ennemi lors du spawn.
    /// </summary>
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
                // Vérifie si on a atteint le beat de vulnérabilité
                if (currentBeat >= targetBeatTime)
                {
                    BecomeVulnerable();
                }
                break;

            case EnemyState.Vulnerable:
                // Vérifie si la fenêtre de vulnérabilité est dépassée
                float elapsed = Time.time - vulnerableStartTime;
                if (elapsed >= vulnerableDuration)
                {
                    Explode();
                }
                break;

            case EnemyState.Exploding:
                // L'animation d'explosion gère la destruction
                break;
        }
    }

    /// <summary>
    /// L'ennemi devient vulnérable — le joueur peut le frapper.
    /// </summary>
    private void BecomeVulnerable()
    {
        currentState = EnemyState.Vulnerable;
        vulnerableStartTime = Time.time;

        if (vulnerableEffect != null)
            vulnerableEffect.SetActive(true);

        // Changement visuel
        if (mainRenderer != null)
            mainRenderer.material.color = Color.white;
    }

    /// <summary>
    /// Tente de frapper cet ennemi. Vérifie le type d'input requis.
    /// </summary>
    /// <param name="inputType">Le type d'input utilisé par le joueur</param>
    /// <returns>Le résultat du timing, ou Miss si pas vulnérable/mauvais input</returns>
    public TimingResult TryHit(EnemyInputType inputType)
    {
        // Pas vulnérable ? Calcul du timing quand même
        if (currentState == EnemyState.Moving)
        {
            // L'ennemi n'est pas encore vulnérable
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

        // Vérifie le type d'input
        if (!IsCorrectInput(inputType))
            return TimingResult.Miss;

        // Mode Spam : compteur de clics
        if (data.requiredInput == EnemyInputType.Spam)
        {
            return HandleSpamHit();
        }

        // Hit normal : évalue le timing
        float targetTime = BeatManager.Instance.BeatToSeconds(targetBeatTime);
        float hitTime = AudioManager.Instance != null ? AudioManager.Instance.MusicTime : Time.time;

        TimingResult result = TimingJudge.Instance != null
            ? TimingJudge.Instance.Judge(hitTime, targetTime)
            : TimingResult.Good;

        // Applique les dégâts
        currentHP--;
        if (currentHP <= 0)
        {
            Die(result);
        }

        return result;
    }

    /// <summary>
    /// Vérifie si l'input du joueur correspond au type requis.
    /// </summary>
    private bool IsCorrectInput(EnemyInputType input)
    {
        return data.requiredInput switch
        {
            EnemyInputType.Any => true, // N'importe quel input est ok
            EnemyInputType.LeftOnly => input == EnemyInputType.LeftOnly,
            EnemyInputType.RightOnly => input == EnemyInputType.RightOnly,
            EnemyInputType.Both => input == EnemyInputType.Both,
            EnemyInputType.Spam => input == EnemyInputType.LeftOnly || input == EnemyInputType.RightOnly || input == EnemyInputType.Any,
            _ => false
        };
    }

    /// <summary>
    /// Gère un clic en mode Spam.
    /// </summary>
    private TimingResult HandleSpamHit()
    {
        if (spamClickCount == 0)
        {
            spamWindowStartTime = Time.time;
        }

        spamClickCount++;

        // Vérifie si le nombre de clics est atteint
        if (spamClickCount >= data.spamClicksRequired)
        {
            // Vérifie si c'est dans la fenêtre de temps
            float elapsed = Time.time - spamWindowStartTime;
            if (elapsed <= data.spamWindowDuration)
            {
                currentHP = 0;
                Die(TimingResult.Good);
                return TimingResult.Good;
            }
            else
            {
                // Trop lent — reset
                spamClickCount = 0;
                return TimingResult.TooLate;
            }
        }

        // Pas encore assez de clics
        return TimingResult.Good;
    }

    /// <summary>
    /// L'ennemi meurt (a été frappé avec succès).
    /// </summary>
    private void Die(TimingResult result)
    {
        currentState = EnemyState.Dead;

        if (vulnerableEffect != null)
            vulnerableEffect.SetActive(false);

        OnEnemyKilled?.Invoke(this, result);

        // Détruire après un petit délai pour laisser les effets
        Destroy(gameObject, 0.1f);
    }

    /// <summary>
    /// L'ennemi explose car il n'a pas été frappé à temps.
    /// Inflige des dégâts au joueur.
    /// </summary>
    private void Explode()
    {
        currentState = EnemyState.Exploding;

        if (vulnerableEffect != null)
            vulnerableEffect.SetActive(false);

        OnEnemyExploded?.Invoke(this);

        // Animation / effets d'explosion ici
        // TODO: Particle effects, screen shake

        Destroy(gameObject, 0.3f);
    }

    /// <summary>
    /// Retourne le temps en secondes du beat cible de cet ennemi.
    /// </summary>
    public float GetTargetTimeInSeconds()
    {
        if (BeatManager.Instance != null)
            return BeatManager.Instance.BeatToSeconds(targetBeatTime);
        return 0f;
    }
}
