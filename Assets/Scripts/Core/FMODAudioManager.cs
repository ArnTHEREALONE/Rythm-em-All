using UnityEngine;
using System;
using FMOD;
using FMOD.Studio;
using FMODUnity;






public class FMODAudioManager : MonoBehaviour
{
    public static FMODAudioManager Instance { get; private set; }

    [Header("=== État (debug) ===")]
    [SerializeField] private string currentEventPath;
    [SerializeField] private bool isPlaying;
    [SerializeField] private float currentTimeSeconds;
    [SerializeField] private int currentTimelinePositionMs;

    
    private EventInstance musicInstance;
    private bool musicInstanceValid;

    
    private FMOD.ChannelGroup masterChannelGroup;
    private int systemSampleRate;

    
    public bool IsPlaying => isPlaying;

    
    public float MusicTime => currentTimeSeconds;

    
    public int TimelinePositionMs => currentTimelinePositionMs;

    
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

        
        RuntimeManager.CoreSystem.getSoftwareFormat(out systemSampleRate, out _, out _);

        
        RuntimeManager.CoreSystem.getMasterChannelGroup(out masterChannelGroup);
    }

    private void Update()
    {
        if (!musicInstanceValid) return;

        
        musicInstance.getPlaybackState(out PLAYBACK_STATE state);
        isPlaying = (state == PLAYBACK_STATE.PLAYING);

        if (isPlaying)
        {
            
            musicInstance.getTimelinePosition(out currentTimelinePositionMs);
            currentTimeSeconds = currentTimelinePositionMs / 1000f;
        }

        
        if (state == PLAYBACK_STATE.STOPPED && isPlaying)
        {
            isPlaying = false;
            OnMusicEnded?.Invoke();
        }
    }

    
    
    

    
    
    
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

    
    
    
    public void PauseMusic()
    {
        if (musicInstanceValid)
        {
            musicInstance.setPaused(true);
            isPlaying = false;
        }
    }

    
    
    
    public void ResumeMusic()
    {
        if (musicInstanceValid)
        {
            musicInstance.setPaused(false);
            isPlaying = true;
        }
    }

    
    
    
    
    public void SetPitch(float pitch)
    {
        if (musicInstanceValid)
        {
            
            pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            musicInstance.setPitch(pitch);
        }
    }

    
    
    
    public void SeekTo(int timelinePositionMs)
    {
        if (musicInstanceValid)
        {
            musicInstance.setTimelinePosition(Mathf.Max(0, timelinePositionMs));
        }
    }

    
    
    
    public void SeekToSeconds(float seconds)
    {
        SeekTo(Mathf.RoundToInt(seconds * 1000f));
    }

    
    
    
    public float GetTimelinePositionSeconds()
    {
        if (!musicInstanceValid) return 0f;
        musicInstance.getTimelinePosition(out int pos);
        return pos / 1000f;
    }

    
    
    
    public int GetTimelinePosition()
    {
        if (!musicInstanceValid) return 0;
        musicInstance.getTimelinePosition(out int pos);
        return pos;
    }

    
    
    
    public ulong GetDSPClock()
    {
        if (!musicInstanceValid) return 0;

        musicInstance.getChannelGroup(out FMOD.ChannelGroup channelGroup);
        channelGroup.getDSPClock(out ulong dspClock, out _);
        return dspClock;
    }

    
    
    
    public double GetDSPTimeSeconds()
    {
        ulong dspClock = GetDSPClock();
        return systemSampleRate > 0 ? (double)dspClock / systemSampleRate : 0.0;
    }

    
    
    
    
    public int GetMusicLengthMs()
    {
        if (!musicInstanceValid) return 0;

        musicInstance.getDescription(out EventDescription desc);
        desc.getLength(out int length);
        return length;
    }

    
    
    
    public float GetMusicLengthSeconds()
    {
        return GetMusicLengthMs() / 1000f;
    }

    
    
    

    
    
    
    public void PlaySFX(EventReference sfxEvent)
    {
        if (!sfxEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(sfxEvent);
        }
    }

    
    
    
    public void PlaySFX(string eventPath)
    {
        if (!string.IsNullOrEmpty(eventPath))
        {
            RuntimeManager.PlayOneShot(eventPath);
        }
    }

    
    
    
    public void PlaySFX(EventReference sfxEvent, Vector3 position)
    {
        if (!sfxEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(sfxEvent, position);
        }
    }

    
    
    

    
    
    
    public void SetMasterVolume(float volume)
    {
        Bus masterBus = RuntimeManager.GetBus("bus:/");
        masterBus.setVolume(Mathf.Clamp01(volume));
    }

    
    
    
    public void SetMusicVolume(float volume)
    {
        Bus musicBus = RuntimeManager.GetBus("bus:/Music");
        musicBus.setVolume(Mathf.Clamp01(volume));
    }

    
    
    
    public void SetSFXVolume(float volume)
    {
        Bus sfxBus = RuntimeManager.GetBus("bus:/SFX");
        sfxBus.setVolume(Mathf.Clamp01(volume));
    }

    
    
    

    private void OnDestroy()
    {
        StopMusic();
    }
}
