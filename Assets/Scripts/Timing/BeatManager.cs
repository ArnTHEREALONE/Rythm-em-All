using UnityEngine;
using System;

/// <summary>
/// Horloge rythmique du jeu. Se synchronise sur le DSP clock FMOD.
/// La musique est la source de vérité — le gameplay suit.
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
    [SerializeField] private float currentTimeSeconds;

    private int lastBeatInt = -1;
    private int lastHalfBeatInt = -1;

    /// <summary>Beat actuel (float, ex: 12.75).</summary>
    public float CurrentBeat => currentBeat;

    /// <summary>BPM de base.</summary>
    public float BPM => bpm;

    /// <summary>Durée d'un beat en secondes (basé sur le BPM de base).</summary>
    public float SecondsPerBeat => 60f / bpm;

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
    /// Démarre l'horloge (appelé quand la musique commence).
    /// </summary>
    public void StartBeat()
    {
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
        if (FMODAudioManager.Instance == null || !FMODAudioManager.Instance.IsPlaying) return;

        // === SOURCE DE VÉRITÉ : position timeline FMOD ===
        // getTimelinePosition() retourne la position en ms, 
        // qui tient déjà compte du pitch (vitesse du jeu).
        // Le gameplay se synchronise SUR la musique.
        currentTimeSeconds = FMODAudioManager.Instance.GetTimelinePositionSeconds();

        // Convertir en beats
        float adjustedTime = currentTimeSeconds - songOffset;
        if (adjustedTime < 0f) adjustedTime = 0f;

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
