using UnityEngine;
using System;

/// <summary>
/// Horloge rythmique du jeu. Fournit le beat actuel et déclenche des events à chaque beat.
/// Utilise AudioSettings.dspTime pour une précision de timing maximale.
/// Le BPM effectif est scalé par la vitesse du jeu.
/// </summary>
public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("=== Configuration ===")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float songOffset = 0f;

    [Header("=== État (debug) ===")]
    [SerializeField] private float currentBeat;
    [SerializeField] private bool isRunning;

    private double dspStartTime;
    private int lastBeatInt = -1;
    private int lastHalfBeatInt = -1;

    /// <summary>Beat actuel (float, ex: 12.75).</summary>
    public float CurrentBeat => currentBeat;

    /// <summary>BPM de base (sans scaling).</summary>
    public float BPM => bpm;

    /// <summary>BPM effectif (avec scaling du SpeedMultiplier).</summary>
    public float EffectiveBPM
    {
        get
        {
            float speed = SpeedMultiplier.Instance != null ? SpeedMultiplier.Instance.CurrentGameSpeed : 1f;
            return bpm * speed;
        }
    }

    /// <summary>Durée d'un beat en secondes (avec scaling).</summary>
    public float SecondsPerBeat => 60f / EffectiveBPM;

    /// <summary>L'horloge est-elle en marche ?</summary>
    public bool IsRunning => isRunning;

    // === Events ===
    /// <summary>Déclenché à chaque beat entier.</summary>
    public event Action<int> OnBeat;

    /// <summary>Déclenché à chaque demi-beat.</summary>
    public event Action<int> OnHalfBeat;

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
    /// Initialise l'horloge avec le BPM et l'offset de la beatmap.
    /// </summary>
    public void Initialize(float bpm, float offset)
    {
        this.bpm = bpm;
        this.songOffset = offset;
        currentBeat = 0f;
        lastBeatInt = -1;
        lastHalfBeatInt = -1;
    }

    /// <summary>
    /// Démarre l'horloge.
    /// </summary>
    public void StartBeat()
    {
        dspStartTime = AudioSettings.dspTime;
        isRunning = true;
        currentBeat = 0f;
        lastBeatInt = -1;
        lastHalfBeatInt = -1;
    }

    /// <summary>
    /// Arrête l'horloge.
    /// </summary>
    public void StopBeat()
    {
        isRunning = false;
    }

    /// <summary>
    /// Met en pause / reprend l'horloge.
    /// </summary>
    public void SetPaused(bool paused)
    {
        isRunning = !paused;
    }

    private void Update()
    {
        if (!isRunning) return;

        // Calcul du beat actuel basé sur le temps de la musique
        float musicTime = 0f;
        if (AudioManager.Instance != null && AudioManager.Instance.IsPlaying)
        {
            musicTime = AudioManager.Instance.MusicTime;
        }
        else
        {
            // Fallback : calcul basé sur DSP time
            musicTime = (float)(AudioSettings.dspTime - dspStartTime);
        }

        // Soustraire l'offset
        float adjustedTime = musicTime - songOffset;
        if (adjustedTime < 0f) adjustedTime = 0f;

        // Convertir en beats (le pitch/vitesse est déjà géré par AudioSource.pitch)
        currentBeat = adjustedTime / (60f / bpm);

        // Déclencher les events de beat
        int currentBeatInt = Mathf.FloorToInt(currentBeat);
        if (currentBeatInt > lastBeatInt)
        {
            lastBeatInt = currentBeatInt;
            OnBeat?.Invoke(currentBeatInt);
        }

        // Déclencher les events de demi-beat
        int currentHalfBeatInt = Mathf.FloorToInt(currentBeat * 2f);
        if (currentHalfBeatInt > lastHalfBeatInt)
        {
            lastHalfBeatInt = currentHalfBeatInt;
            OnHalfBeat?.Invoke(currentHalfBeatInt);
        }
    }

    /// <summary>
    /// Convertit un temps en beats vers un temps en secondes.
    /// </summary>
    public float BeatToSeconds(float beat)
    {
        return songOffset + beat * (60f / bpm);
    }

    /// <summary>
    /// Convertit un temps en secondes vers un temps en beats.
    /// </summary>
    public float SecondsToBeat(float seconds)
    {
        return (seconds - songOffset) / (60f / bpm);
    }

    /// <summary>
    /// Retourne le temps en secondes du prochain beat (entier).
    /// </summary>
    public float GetNextBeatTime()
    {
        int nextBeat = Mathf.CeilToInt(currentBeat);
        return BeatToSeconds(nextBeat);
    }
}
