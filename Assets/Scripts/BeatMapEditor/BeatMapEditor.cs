using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

// Création, chargement, sauvegarde des beatmaps
public class BeatMapEditor : MonoBehaviour
{
    public static BeatMapEditor Instance { get; private set; }

    [Header("État")]
    [SerializeField] private BeatMapData currentMap;
    [SerializeField] private bool isPlaying;
    [SerializeField] private float currentBeat;
    [SerializeField] private int selectedLane;

    [Header("Références")]
    public EditorTimeline timeline;
    public EditorGrid grid;
    public EditorControls controls;
    public EditorMetronome metronome;
    public EditorMusicLibrary musicLibrary;
    public EditorMusicImporter musicImporter;

    public BeatMapData CurrentMap => currentMap;

    public float CurrentBeat => currentBeat;

    public int SelectedLane => selectedLane;

    public bool IsPlaying => isPlaying;

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
            songName = "New Map",
            musicFileName = "",
            bpm = 120f,
            songOffset = 0f,
            notes = new List<BeatNote>()
        };

        currentBeat = 0f;
        selectedLane = 0;
        isPlaying = false;

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

        currentBeat = 0f;
        isPlaying = false;

        RefreshUI();
        Debug.Log($"BeatMapEditor: Loaded '{currentMap.songName}' ({currentMap.notes.Count} notes)");
    }

    public void SaveMap()
    {
        if (currentMap == null) return;

        string dir = Path.Combine(Application.persistentDataPath, "BeatMaps");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string safeName = currentMap.songName.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
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


    public void SelectMusic(string musicFileName)
    {
        if (currentMap != null)
        {
            currentMap.musicFileName = musicFileName;
            currentMap.songName = Path.GetFileNameWithoutExtension(musicFileName);
            RefreshUI();
        }
    }


    public void PlaceNote(EnemyInputType inputType)
    {
        if (currentMap == null) return;

        BeatNote existing = currentMap.notes.Find(n =>
            Mathf.Approximately(n.beatTime, currentBeat) && n.spawnerIndex == selectedLane);

        if (existing != null)
        {
            currentMap.notes.Remove(existing);
        }

        BeatNote note = new BeatNote(currentBeat, selectedLane, inputType);
        currentMap.notes.Add(note);

        currentMap.notes.Sort((a, b) => a.beatTime.CompareTo(b.beatTime));

        RefreshUI();
    }

    public void DeleteNoteAtCursor()
    {
        if (currentMap == null) return;

        BeatNote note = currentMap.notes.Find(n =>
            Mathf.Approximately(n.beatTime, currentBeat) && n.spawnerIndex == selectedLane);

        if (note != null)
        {
            currentMap.notes.Remove(note);
            RefreshUI();
        }
    }

    public void MoveBeat(float deltaBeats)
    {
        currentBeat = Mathf.Max(0f, currentBeat + deltaBeats);
        RefreshUI();

        if (isPlaying && AudioManager.Instance != null && currentMap != null)
        {
            float time = currentMap.BeatToSeconds(currentBeat);
            AudioManager.Instance.SeekTo(time);
        }
    }

    public void MoveLane(int delta)
    {
        selectedLane = Mathf.Max(0, selectedLane + delta);
        RefreshUI();
    }

    public void TogglePlayback()
    {
        if (currentMap == null || string.IsNullOrEmpty(currentMap.musicFileName)) return;

        if (isPlaying)
        {
            isPlaying = false;
            AudioManager.Instance?.PauseMusic();
        }
        else
        {
            isPlaying = true;

            string musicPath = Path.Combine(Application.persistentDataPath, "Music", currentMap.musicFileName);

            if (AudioManager.Instance != null)
            {
                if (AudioManager.Instance.IsPlaying)
                {
                    AudioManager.Instance.ResumeMusic();
                }
                else
                {
                    AudioManager.Instance.LoadAudioClip(musicPath, (clip) =>
                    {
                        if (clip != null)
                        {
                            AudioManager.Instance.PlayMusic(clip);
                            AudioManager.Instance.SetPitch(1f);
                            AudioManager.Instance.SeekTo(currentMap.BeatToSeconds(currentBeat));
                        }
                    });
                }
            }
        }
    }

    private void Update()
    {
        if (isPlaying && AudioManager.Instance != null && AudioManager.Instance.IsPlaying)
        {
            float musicTime = AudioManager.Instance.MusicTime;
            currentBeat = currentMap.SecondsToBeat(musicTime);

            if (metronome != null)
                metronome.UpdateBeat(currentBeat);

            RefreshUI();

            if (musicTime >= AudioManager.Instance.MusicLength - 0.1f)
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
        AudioManager.Instance?.StopMusic();
        SceneManager.LoadScene("MainMenu");
    }
}
