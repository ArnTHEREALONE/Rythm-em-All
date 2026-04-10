using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Contrôleur principal de l'éditeur de beatmap.
/// Utilise FMOD pour la lecture musicale et le MusicDatabase pour la sélection.
/// </summary>
public class BeatMapEditor : MonoBehaviour
{
    public static BeatMapEditor Instance { get; private set; }

    [Header("=== État ===")]
    [SerializeField] private BeatMapData currentMap;
    [SerializeField] private bool isPlaying;
    [SerializeField] private float currentBeat;
    [SerializeField] private int selectedLane;

    [Header("=== Références ===")]
    public EditorTimeline timeline;
    public EditorGrid grid;
    public EditorControls controls;
    public EditorMetronome metronome;
    public EditorMusicLibrary musicLibrary;
    public MusicDatabase musicDatabase;

    /// <summary>BeatMap en cours d'édition.</summary>
    public BeatMapData CurrentMap => currentMap;

    /// <summary>Beat actuel dans l'éditeur.</summary>
    public float CurrentBeat => currentBeat;

    /// <summary>Lane actuellement sélectionnée.</summary>
    public int SelectedLane => selectedLane;

    /// <summary>Est en lecture ?</summary>
    public bool IsPlaying => isPlaying;

    /// <summary>L'entrée musique sélectionnée.</summary>
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

    /// <summary>
    /// Crée une nouvelle beatmap vierge.
    /// </summary>
    public void NewMap()
    {
        currentMap = new BeatMapData
        {
            songName = "New Map",
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

    /// <summary>
    /// Charge une beatmap depuis un fichier JSON.
    /// </summary>
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

        // Retrouver le MusicEntry associé
        if (musicDatabase != null && !string.IsNullOrEmpty(currentMap.fmodEventPath))
        {
            SelectedMusicEntry = musicDatabase.GetByEventPath(currentMap.fmodEventPath);
        }

        currentBeat = 0f;
        isPlaying = false;

        RefreshUI();
        Debug.Log($"BeatMapEditor: Loaded '{currentMap.songName}' ({currentMap.notes.Count} notes)");
    }

    /// <summary>
    /// Sauvegarde la beatmap en JSON.
    /// </summary>
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

    /// <summary>
    /// Définit le BPM de la beatmap.
    /// </summary>
    public void SetBPM(float bpm)
    {
        if (currentMap != null && bpm > 0)
        {
            currentMap.bpm = bpm;
            RefreshUI();
        }
    }

    /// <summary>
    /// Sélectionne une musique depuis le MusicDatabase.
    /// </summary>
    public void SelectMusic(MusicEntry entry)
    {
        if (currentMap == null || entry == null) return;

        SelectedMusicEntry = entry;
        currentMap.fmodEventPath = entry.fmodEventPath;
        currentMap.songName = entry.displayName;
        currentMap.bpm = entry.bpm;
        currentMap.songOffset = entry.songOffset;

        RefreshUI();
    }

    /// <summary>
    /// Place une note à la position actuelle (beat + lane sélectionnés).
    /// </summary>
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

    /// <summary>
    /// Supprime la note à la position actuelle.
    /// </summary>
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

    /// <summary>
    /// Navigation : avancer/reculer dans le temps.
    /// </summary>
    public void MoveBeat(float deltaBeats)
    {
        currentBeat = Mathf.Max(0f, currentBeat + deltaBeats);
        RefreshUI();

        // Si en lecture, seek dans FMOD
        if (isPlaying && FMODAudioManager.Instance != null && currentMap != null)
        {
            float time = currentMap.BeatToSeconds(currentBeat);
            FMODAudioManager.Instance.SeekToSeconds(time);
        }
    }

    /// <summary>
    /// Navigation : changer de lane.
    /// </summary>
    public void MoveLane(int delta)
    {
        selectedLane = Mathf.Max(0, selectedLane + delta);
        RefreshUI();
    }

    /// <summary>
    /// Toggle play/pause via FMOD.
    /// </summary>
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
                    FMODAudioManager.Instance.SetPitch(1f); // Pas de speed multi dans l'éditeur
                    FMODAudioManager.Instance.SeekToSeconds(currentMap.BeatToSeconds(currentBeat));
                }
            }
        }
    }

    private void Update()
    {
        // En lecture : sync le beat actuel avec FMOD
        if (isPlaying && FMODAudioManager.Instance != null && FMODAudioManager.Instance.IsPlaying)
        {
            float musicTime = FMODAudioManager.Instance.GetTimelinePositionSeconds();
            currentBeat = currentMap.SecondsToBeat(musicTime);

            // Métronome
            if (metronome != null)
                metronome.UpdateBeat(currentBeat);

            RefreshUI();

            // Si la musique est finie
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

    /// <summary>
    /// Retour au menu principal.
    /// </summary>
    public void ReturnToMenu()
    {
        FMODAudioManager.Instance?.StopMusic();
        SceneManager.LoadScene("MainMenu");
    }
}
