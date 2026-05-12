using UnityEngine;
using System;





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

    
    public float CurrentBeat => currentBeat;

    
    public float BPM => bpm;

    
    public float SecondsPerBeat => 60f / bpm;

    
    public bool IsRunning => isRunning;

    
    
    public event Action<int> OnBeat;

    
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

    
    
    
    public void Initialize(float bpm, float offset)
    {
        this.bpm = bpm;
        this.songOffset = offset;
        currentBeat = 0f;
        lastBeatInt = -1;
        lastHalfBeatInt = -1;
    }

    
    
    
    public void StartBeat()
    {
        isRunning = true;
        currentBeat = 0f;
        lastBeatInt = -1;
        lastHalfBeatInt = -1;
    }

    
    
    
    public void StopBeat()
    {
        isRunning = false;
    }

    
    
    
    public void SetPaused(bool paused)
    {
        isRunning = !paused;
    }

    private void Update()
    {
        if (!isRunning) return;
        if (FMODAudioManager.Instance == null || !FMODAudioManager.Instance.IsPlaying) return;

        
        
        
        
        currentTimeSeconds = FMODAudioManager.Instance.GetTimelinePositionSeconds();

        
        float adjustedTime = currentTimeSeconds - songOffset;
        if (adjustedTime < 0f) adjustedTime = 0f;

        currentBeat = adjustedTime / (60f / bpm);

        
        int currentBeatInt = Mathf.FloorToInt(currentBeat);
        if (currentBeatInt > lastBeatInt)
        {
            lastBeatInt = currentBeatInt;
            OnBeat?.Invoke(currentBeatInt);
        }

        
        int currentHalfBeatInt = Mathf.FloorToInt(currentBeat * 2f);
        if (currentHalfBeatInt > lastHalfBeatInt)
        {
            lastHalfBeatInt = currentHalfBeatInt;
            OnHalfBeat?.Invoke(currentHalfBeatInt);
        }
    }

    
    
    
    public float BeatToSeconds(float beat)
    {
        return songOffset + beat * (60f / bpm);
    }

    
    
    
    public float SecondsToBeat(float seconds)
    {
        return (seconds - songOffset) / (60f / bpm);
    }

    
    
    
    public float GetNextBeatTime()
    {
        int nextBeat = Mathf.CeilToInt(currentBeat);
        return BeatToSeconds(nextBeat);
    }
}
