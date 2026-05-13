using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;





public class BeatMapEditor : MonoBehaviour
{
    public static BeatMapEditor Instance { get; private set; }

    [Header("=== État ===")]
    [SerializeField] private BeatMapData currentMap;
    [SerializeField] private bool isPlaying;
    [SerializeField] private float currentBeat;
    [SerializeField] private int selectedLane;

    [Header("=== Configuration ===")]
    [Tooltip("Nombre de lanes (= nombre de spawners dans la scène Game)")]
    public int laneCount = 12;

    [Header("=== Références ===")]
    public EditorTimeline timeline;
    public EditorGrid grid;
    public EditorControls controls;
    public EditorMetronome metronome;
    public EditorMusicLibrary musicLibrary;
    public EditorGestionPanel gestionPanel;
    public MusicDatabase musicDatabase;

    
    public BeatMapData CurrentMap => currentMap;

    
    public float CurrentBeat => currentBeat;

    
    public int SelectedLane => selectedLane;

    
    public bool IsPlaying => isPlaying;

    
    public MusicEntry SelectedMusicEntry { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        NewMap();
    }

    
    
    
    public void NewMap()
    {
        currentMap = new BeatMapData
        {
            mapName = "New Map",
            songName = "",
            fmodEventPath = "",
            bpm = 120f,
            songOffset = 0f,
            notes = new List<BeatNote>()
        };

        currentBeat = 0f;
        selectedLane = 0;
        isPlaying = false;
        SelectedMusicEntry = null;

        RefreshUI();
    }

    
    
    
    public void LoadMap(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"BeatMapEditor: File not found: {filePath}");
            return;
        }

        string json = File.ReadAllText(filePath);
        currentMap = BeatMapData.FromJson(json);

        if (currentMap == null)
        {
            Debug.LogError("BeatMapEditor: Failed to parse beatmap!");
            NewMap();
            return;
        }

        
        if (musicDatabase != null && !string.IsNullOrEmpty(currentMap.fmodEventPath))
        {
            SelectedMusicEntry = musicDatabase.GetByEventPath(currentMap.fmodEventPath);
            if (musicLibrary != null && SelectedMusicEntry != null)
            {
                
            }
        }

        currentBeat = 0f;
        isPlaying = false;

        RefreshUI();
        Debug.Log($"BeatMapEditor: Loaded '{currentMap.mapName}' ({currentMap.notes.Count} notes)");
    }

    
    
    
    public void SaveMap()
    {
        if (currentMap == null) return;
        SaveMapWithName(string.IsNullOrEmpty(currentMap.mapName) ? "Map sans nom" : currentMap.mapName);
    }

    
    
    
    
    public void SaveMapWithName(string mapName)
    {
        if (currentMap == null) return;

        
        currentMap.mapName = mapName;

        string dir = Path.Combine(Application.persistentDataPath, "BeatMaps");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string safeName = mapName.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
        string filePath = Path.Combine(dir, safeName + ".json");

        string json = currentMap.ToJson();
        File.WriteAllText(filePath, json);

        Debug.Log($"BeatMapEditor: Saved to {filePath}");
    }

    
    
    
    public void SetBPM(float bpm)
    {
        if (currentMap != null && bpm > 0)
        {
            currentMap.bpm = bpm;
            RefreshUI();
        }
    }

    
    
    
    
    public void SelectMusic(MusicEntry entry)
    {
        if (currentMap == null || entry == null) return;

        SelectedMusicEntry = entry;
        currentMap.fmodEventPath = entry.fmodEventPath;
        currentMap.songName = entry.displayName;
        currentMap.bpm = entry.bpm;
        currentMap.songOffset = entry.songOffset;

        
        currentBeat = 0f;
        isPlaying = false;

        RefreshUI();
    }

    
    
    
    public void PlaceNote(EnemyInputType inputType)
    {
        if (currentMap == null) return;

        float snappedBeat = Mathf.Round(currentBeat);

        BeatNote existing = currentMap.notes.Find(n =>
            Mathf.Approximately(n.beatTime, snappedBeat) && n.spawnerIndex == selectedLane);

        if (existing != null)
        {
            currentMap.notes.Remove(existing);
        }

        BeatNote note = new BeatNote(snappedBeat, selectedLane, inputType);
        currentMap.notes.Add(note);
        currentMap.notes.Sort((a, b) => a.beatTime.CompareTo(b.beatTime));

        RefreshUI();
    }

    
    
    
    
    public void PlaceNoteAt(float beat, int lane, EnemyInputType inputType)
    {
        if (currentMap == null) return;
        if (lane < 0 || lane >= laneCount) return;

        
        float snappedBeat = Mathf.Round(beat);

        BeatNote existing = currentMap.notes.Find(n =>
            Mathf.Approximately(n.beatTime, snappedBeat) && n.spawnerIndex == lane);

        if (existing != null)
        {
            currentMap.notes.Remove(existing);
        }

        BeatNote note = new BeatNote(snappedBeat, lane, inputType);
        currentMap.notes.Add(note);
        currentMap.notes.Sort((a, b) => a.beatTime.CompareTo(b.beatTime));

        RefreshUI();
    }

    
    
    
    public void DeleteNoteAtCursor()
    {
        if (currentMap == null) return;

        float snappedBeat = Mathf.Round(currentBeat);
        DeleteNoteAt(snappedBeat, selectedLane);
    }

    /// <summary>
    /// Supprime une note à une position spécifique.
    /// </summary>
    public void DeleteNoteAt(float beat, int lane)
    {
        if (currentMap == null) return;

        float snappedBeat = Mathf.Round(beat);
        BeatNote note = currentMap.notes.Find(n =>
            Mathf.Approximately(n.beatTime, snappedBeat) && n.spawnerIndex == lane);

        if (note != null)
        {
            currentMap.notes.Remove(note);
            RefreshUI();
        }
    }

    
    
    
    public void DeleteNote(BeatNote note)
    {
        if (currentMap == null || note == null) return;
        currentMap.notes.Remove(note);
        RefreshUI();
    }

    
    
    
    public void MoveBeat(float deltaBeats)
    {
        currentBeat = Mathf.Max(0f, currentBeat + deltaBeats);
        RefreshUI();

        
        if (isPlaying && FMODAudioManager.Instance != null && currentMap != null)
        {
            float time = currentMap.BeatToSeconds(currentBeat);
            FMODAudioManager.Instance.SeekToSeconds(time);
        }
    }

    
    
    
    public void GoToBeat(float beat)
    {
        currentBeat = Mathf.Max(0f, beat);
        RefreshUI();

        if (isPlaying && FMODAudioManager.Instance != null && currentMap != null)
        {
            float time = currentMap.BeatToSeconds(currentBeat);
            FMODAudioManager.Instance.SeekToSeconds(time);
        }
    }

    
    
    
    public void MoveLane(int delta)
    {
        selectedLane = Mathf.Clamp(selectedLane + delta, 0, laneCount - 1);
        RefreshUI();
    }

    
    
    
    public void TogglePlayback()
    {
        if (currentMap == null || SelectedMusicEntry == null) return;

        if (isPlaying)
        {
            isPlaying = false;
            FMODAudioManager.Instance?.PauseMusic();
        }
        else
        {
            isPlaying = true;

            if (FMODAudioManager.Instance != null)
            {
                if (FMODAudioManager.Instance.IsPlaying)
                {
                    FMODAudioManager.Instance.ResumeMusic();
                }
                else
                {
                    FMODAudioManager.Instance.PlayMusic(SelectedMusicEntry.fmodEvent);
                    FMODAudioManager.Instance.SetPitch(1f); 
                    FMODAudioManager.Instance.SeekToSeconds(currentMap.BeatToSeconds(currentBeat));
                }
            }
        }
    }

    private void Update()
    {
        
        if (isPlaying && FMODAudioManager.Instance != null && FMODAudioManager.Instance.IsPlaying)
        {
            float musicTime = FMODAudioManager.Instance.GetTimelinePositionSeconds();
            currentBeat = currentMap.SecondsToBeat(musicTime);

            
            if (metronome != null)
                metronome.UpdateBeat(currentBeat);

            RefreshUI();

            
            float musicLength = FMODAudioManager.Instance.GetMusicLengthSeconds();
            if (musicLength > 0 && musicTime >= musicLength - 0.1f)
            {
                isPlaying = false;
            }
        }
    }

    private void RefreshUI()
    {
        if (timeline != null) timeline.Refresh();
        if (grid != null) grid.Refresh();
    }

    
    
    
    public void ReturnToMenu()
    {
        FMODAudioManager.Instance?.StopMusic();
        SceneManager.LoadScene("MainMenu");
    }
}
