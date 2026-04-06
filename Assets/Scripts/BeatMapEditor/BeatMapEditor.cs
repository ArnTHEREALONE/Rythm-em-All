using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Contrôleur principal de l'éditeur de beatmap.
/// Gère la création, le chargement, la sauvegarde des beatmaps,
/// et le placement des notes via les inputs.
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
    public EditorMusicImporter musicImporter;

    /// <summary>BeatMap en cours d'édition.</summary>
    public BeatMapData CurrentMap => currentMap;

    /// <summary>Beat actuel dans l'éditeur.</summary>
    public float CurrentBeat => currentBeat;

    /// <summary>Lane actuellement sélectionnée.</summary>
    public int SelectedLane => selectedLane;

    /// <summary>Est en lecture ?</summary>
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

    /// <summary>
    /// Crée une nouvelle beatmap vierge.
    /// </summary>
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

        // Nom de fichier basé sur le nom de la chanson
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
    /// Sélectionne un fichier musique pour la beatmap.
    /// </summary>
    public void SelectMusic(string musicFileName)
    {
        if (currentMap != null)
        {
            currentMap.musicFileName = musicFileName;
            currentMap.songName = Path.GetFileNameWithoutExtension(musicFileName);
            RefreshUI();
        }
    }

    /// <summary>
    /// Place une note à la position actuelle (beat + lane sélectionnés).
    /// </summary>
    public void PlaceNote(EnemyInputType inputType)
    {
        if (currentMap == null) return;

        // Vérifier s'il y a déjà une note à cette position
        BeatNote existing = currentMap.notes.Find(n =>
            Mathf.Approximately(n.beatTime, currentBeat) && n.spawnerIndex == selectedLane);

        if (existing != null)
        {
            // Supprimer l'ancienne note et la remplacer
            currentMap.notes.Remove(existing);
        }

        BeatNote note = new BeatNote(currentBeat, selectedLane, inputType);
        currentMap.notes.Add(note);

        // Trier par beatTime
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

        // Si en lecture, seek dans la musique
        if (isPlaying && AudioManager.Instance != null && currentMap != null)
        {
            float time = currentMap.BeatToSeconds(currentBeat);
            AudioManager.Instance.SeekTo(time);
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
    /// Toggle play/pause.
    /// </summary>
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

            // Charger et jouer la musique si pas encore fait
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
                            AudioManager.Instance.SetPitch(1f); // Pas de speed multiplier dans l'éditeur
                            AudioManager.Instance.SeekTo(currentMap.BeatToSeconds(currentBeat));
                        }
                    });
                }
            }
        }
    }

    private void Update()
    {
        // En lecture : sync le beat actuel avec la musique
        if (isPlaying && AudioManager.Instance != null && AudioManager.Instance.IsPlaying)
        {
            float musicTime = AudioManager.Instance.MusicTime;
            currentBeat = currentMap.SecondsToBeat(musicTime);

            // Métronome
            if (metronome != null)
                metronome.UpdateBeat(currentBeat);

            RefreshUI();

            // Si la musique est finie
            if (musicTime >= AudioManager.Instance.MusicLength - 0.1f)
            {
                isPlaying = false;
            }
        }
    }

    /// <summary>
    /// Met à jour tous les éléments visuels de l'éditeur.
    /// </summary>
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
        AudioManager.Instance?.StopMusic();
        SceneManager.LoadScene("MainMenu");
    }
}
