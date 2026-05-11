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

    /// <summary>
    /// Charge une beatmap depuis un fichier JSON (chemin complet).
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
            if (musicLibrary != null && SelectedMusicEntry != null)
            {
                // Optionally highlight in library if logic exists
            }
        }

        currentBeat = 0f;
        isPlaying = false;

        RefreshUI();
        Debug.Log($"BeatMapEditor: Loaded '{currentMap.mapName}' ({currentMap.notes.Count} notes)");
    }

    /// <summary>
    /// Sauvegarde la beatmap en JSON avec le nom actuel de mapName.
    /// </summary>
    public void SaveMap()
    {
        if (currentMap == null) return;
        SaveMapWithName(string.IsNullOrEmpty(currentMap.mapName) ? "Map sans nom" : currentMap.mapName);
    }

    /// <summary>
    /// Sauvegarde la beatmap en JSON avec un nom personnalisé.
    /// Appelé par EditorGestionPanel.
    /// </summary>
    public void SaveMapWithName(string mapName)
    {
        if (currentMap == null) return;

        // Mettre à jour le nom de la map
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
    /// Crée une timeline vide pour cette musique.
    /// </summary>
    public void SelectMusic(MusicEntry entry)
    {
        if (currentMap == null || entry == null) return;

        SelectedMusicEntry = entry;
        currentMap.fmodEventPath = entry.fmodEventPath;
        currentMap.songName = entry.displayName;
        currentMap.bpm = entry.bpm;
        currentMap.songOffset = entry.songOffset;

        // Réinitialiser la position
        currentBeat = 0f;
        isPlaying = false;

        RefreshUI();
    }

    /// <summary>
    /// Place une note à la position actuelle (beat + lane sélectionnés).
    /// </summary>
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

    /// <summary>
    /// Place une note à une position spécifique (beat + lane).
    /// Utilisé par le drag & drop sur la timeline.
    /// </summary>
    public void PlaceNoteAt(float beat, int lane, EnemyInputType inputType)
    {
        if (currentMap == null) return;
        if (lane < 0 || lane >= laneCount) return;

        // Snapper sur le beat le plus proche
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
    /// Supprime une note spécifique (utilisé par le drag & drop).
    /// </summary>
    public void DeleteNote(BeatNote note)
    {
        if (currentMap == null || note == null) return;
        currentMap.notes.Remove(note);
        RefreshUI();
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
    /// Navigation : aller directement à un beat précis.
    /// </summary>
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

    /// <summary>
    /// Navigation : changer de lane.
    /// </summary>
    public void MoveLane(int delta)
    {
        selectedLane = Mathf.Clamp(selectedLane + delta, 0, laneCount - 1);
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
