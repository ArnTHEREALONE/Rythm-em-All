using UnityEngine;
using System;
using FMOD;
using FMOD.Studio;
using FMODUnity;

/// <summary>
/// Gestionnaire audio basé sur FMOD. Singleton.
/// Remplace l'ancien AudioManager Unity.
/// La DSP clock FMOD est la source de vérité pour le timing du gameplay.
/// </summary>
public class FMODAudioManager : MonoBehaviour
{
    public static FMODAudioManager Instance { get; private set; }

    [Header("=== État (debug) ===")]
    [SerializeField] private string currentEventPath;
    [SerializeField] private bool isPlaying;
    [SerializeField] private float currentTimeSeconds;
    [SerializeField] private int currentTimelinePositionMs;

    // Instance FMOD de la musique en cours
    private EventInstance musicInstance;
    private bool musicInstanceValid;

    // DSP clock pour timing précis
    private FMOD.ChannelGroup masterChannelGroup;
    private int systemSampleRate;

    /// <summary>Est-ce que la musique joue ?</summary>
    public bool IsPlaying => isPlaying;

    /// <summary>Temps actuel de la musique en secondes (depuis la timeline FMOD).</summary>
    public float MusicTime => currentTimeSeconds;

    /// <summary>Position dans la timeline FMOD en millisecondes.</summary>
    public int TimelinePositionMs => currentTimelinePositionMs;

    // === Events ===
    public event Action OnMusicStarted;
    public event Action OnMusicStopped;
    public event Action OnMusicEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Récupérer le sample rate système pour les calculs DSP
        RuntimeManager.CoreSystem.getSoftwareFormat(out systemSampleRate, out _, out _);

        // Récupérer le master channel group pour la DSP clock
        RuntimeManager.CoreSystem.getMasterChannelGroup(out masterChannelGroup);
    }

    private void Update()
    {
        if (!musicInstanceValid) return;

        // Vérifier si la musique joue
        musicInstance.getPlaybackState(out PLAYBACK_STATE state);
        isPlaying = (state == PLAYBACK_STATE.PLAYING);

        if (isPlaying)
        {
            // Lire la position dans la timeline FMOD (en ms)
            musicInstance.getTimelinePosition(out currentTimelinePositionMs);
            currentTimeSeconds = currentTimelinePositionMs / 1000f;
        }

        // Détecter la fin de la musique
        if (state == PLAYBACK_STATE.STOPPED && isPlaying)
        {
            isPlaying = false;
            OnMusicEnded?.Invoke();
        }
    }

    // =========================================================================
    // MUSIQUE
    // =========================================================================

    /// <summary>
    /// Joue une musique via son EventReference FMOD.
    /// </summary>
    public void PlayMusic(EventReference eventRef)
    {
        StopMusic();

        musicInstance = RuntimeManager.CreateInstance(eventRef);
        musicInstance.start();
        musicInstanceValid = true;
        isPlaying = true;
        currentEventPath = eventRef.ToString();

        OnMusicStarted?.Invoke();
    }

    /// <summary>
    /// Joue une musique via son chemin d'event FMOD.
    /// </summary>
    public void PlayMusic(string eventPath)
    {
        StopMusic();

        musicInstance = RuntimeManager.CreateInstance(eventPath);
        musicInstance.start();
        musicInstanceValid = true;
        isPlaying = true;
        currentEventPath = eventPath;

        OnMusicStarted?.Invoke();
    }

    /// <summary>
    /// Arrête la musique.
    /// </summary>
    public void StopMusic()
    {
        if (musicInstanceValid)
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.release();
            musicInstanceValid = false;
            isPlaying = false;
            currentTimeSeconds = 0f;
            currentTimelinePositionMs = 0;

            OnMusicStopped?.Invoke();
        }
    }

    /// <summary>
    /// Met la musique en pause.
    /// </summary>
    public void PauseMusic()
    {
        if (musicInstanceValid)
        {
            musicInstance.setPaused(true);
            isPlaying = false;
        }
    }

    /// <summary>
    /// Reprend la musique après une pause.
    /// </summary>
    public void ResumeMusic()
    {
        if (musicInstanceValid)
        {
            musicInstance.setPaused(false);
            isPlaying = true;
        }
    }

    /// <summary>
    /// Définit le pitch de la musique. 1.0 = normal, 2.0 = double vitesse.
    /// Appelé par SpeedMultiplier quand la vitesse du jeu change.
    /// </summary>
    public void SetPitch(float pitch)
    {
        if (musicInstanceValid)
        {
            // Clamp entre 0.1 et 3.0 pour éviter les extrêmes
            pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            musicInstance.setPitch(pitch);
        }
    }

    /// <summary>
    /// Seek à une position dans la timeline FMOD (en millisecondes).
    /// </summary>
    public void SeekTo(int timelinePositionMs)
    {
        if (musicInstanceValid)
        {
            musicInstance.setTimelinePosition(Mathf.Max(0, timelinePositionMs));
        }
    }

    /// <summary>
    /// Seek à une position en secondes.
    /// </summary>
    public void SeekToSeconds(float seconds)
    {
        SeekTo(Mathf.RoundToInt(seconds * 1000f));
    }

    /// <summary>
    /// Retourne la position dans la timeline en secondes (précision FMOD).
    /// </summary>
    public float GetTimelinePositionSeconds()
    {
        if (!musicInstanceValid) return 0f;
        musicInstance.getTimelinePosition(out int pos);
        return pos / 1000f;
    }

    /// <summary>
    /// Retourne la position dans la timeline en millisecondes.
    /// </summary>
    public int GetTimelinePosition()
    {
        if (!musicInstanceValid) return 0;
        musicInstance.getTimelinePosition(out int pos);
        return pos;
    }

    /// <summary>
    /// Retourne la DSP clock de l'instance musicale (ultra précis).
    /// </summary>
    public ulong GetDSPClock()
    {
        if (!musicInstanceValid) return 0;

        musicInstance.getChannelGroup(out FMOD.ChannelGroup channelGroup);
        channelGroup.getDSPClock(out ulong dspClock, out _);
        return dspClock;
    }

    /// <summary>
    /// Retourne le temps DSP en secondes — la source de vérité pour le timing.
    /// </summary>
    public double GetDSPTimeSeconds()
    {
        ulong dspClock = GetDSPClock();
        return systemSampleRate > 0 ? (double)dspClock / systemSampleRate : 0.0;
    }

    /// <summary>
    /// Retourne la durée totale de l'event en cours (en ms).
    /// Note: Nécessite que l'event FMOD ait une longueur définie.
    /// </summary>
    public int GetMusicLengthMs()
    {
        if (!musicInstanceValid) return 0;

        musicInstance.getDescription(out EventDescription desc);
        desc.getLength(out int length);
        return length;
    }

    /// <summary>
    /// Durée totale en secondes.
    /// </summary>
    public float GetMusicLengthSeconds()
    {
        return GetMusicLengthMs() / 1000f;
    }

    // =========================================================================
    // SFX
    // =========================================================================

    /// <summary>
    /// Joue un effet sonore one-shot via FMOD EventReference.
    /// </summary>
    public void PlaySFX(EventReference sfxEvent)
    {
        if (!sfxEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(sfxEvent);
        }
    }

    /// <summary>
    /// Joue un effet sonore one-shot via chemin d'event.
    /// </summary>
    public void PlaySFX(string eventPath)
    {
        if (!string.IsNullOrEmpty(eventPath))
        {
            RuntimeManager.PlayOneShot(eventPath);
        }
    }

    /// <summary>
    /// Joue un SFX à une position 3D.
    /// </summary>
    public void PlaySFX(EventReference sfxEvent, Vector3 position)
    {
        if (!sfxEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(sfxEvent, position);
        }
    }

    // =========================================================================
    // VOLUMES (via FMOD Bus)
    // =========================================================================

    /// <summary>
    /// Définit le volume master (bus:/).
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        Bus masterBus = RuntimeManager.GetBus("bus:/");
        masterBus.setVolume(Mathf.Clamp01(volume));
    }

    /// <summary>
    /// Définit le volume musique (bus:/Music).
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        Bus musicBus = RuntimeManager.GetBus("bus:/Music");
        musicBus.setVolume(Mathf.Clamp01(volume));
    }

    /// <summary>
    /// Définit le volume SFX (bus:/SFX).
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        Bus sfxBus = RuntimeManager.GetBus("bus:/SFX");
        sfxBus.setVolume(Mathf.Clamp01(volume));
    }

    // =========================================================================
    // NETTOYAGE
    // =========================================================================

    private void OnDestroy()
    {
        StopMusic();
    }
}
