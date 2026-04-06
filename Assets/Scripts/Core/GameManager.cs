using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

/// <summary>
/// Gestionnaire principal du gameplay. Gère le flow d'une partie :
/// chargement de la beatmap, initialisation, musique, pause, game over, victoire.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Loading,
        WaitingToStart,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    [Header("=== Références ===")]
    public GameConfig gameConfig;
    public PlayerConfig playerConfig;
    public PlayerController player;

    [Header("=== État ===")]
    [SerializeField] private GameState currentState = GameState.Loading;

    [Header("=== Délai ===")]
    [Tooltip("Délai avant le début de la musique (secondes)")]
    public float startDelay = 3f;

    /// <summary>État actuel du jeu.</summary>
    public GameState CurrentState => currentState;

    /// <summary>BeatMap actuellement chargée (définie avant le chargement de scène).</summary>
    public static BeatMapData SelectedBeatMap { get; set; }

    /// <summary>Chemin du fichier musique sélectionné.</summary>
    public static string SelectedMusicPath { get; set; }

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
        // Distribuer les configs aux singletons
        if (SpeedMultiplier.Instance != null)
            SpeedMultiplier.Instance.config = gameConfig;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.config = gameConfig;

        if (TimingJudge.Instance != null)
            TimingJudge.Instance.config = gameConfig;

        // Initialiser le joueur
        if (player != null && playerConfig != null)
            player.Initialize(playerConfig);

        // Écouter la mort du joueur
        if (player != null && player.health != null)
            player.health.OnDeath += HandlePlayerDeath;

        // Charger la beatmap
        if (SelectedBeatMap != null)
        {
            StartCoroutine(LoadAndStart());
        }
        else
        {
            Debug.LogWarning("GameManager: No beatmap selected! Loading test map...");
            // Mode test : créer une beatmap simple
            LoadTestBeatMap();
        }
    }

    /// <summary>
    /// Charge la musique et démarre la partie.
    /// </summary>
    private IEnumerator LoadAndStart()
    {
        currentState = GameState.Loading;

        // Charger la musique
        string musicPath = SelectedMusicPath;
        if (string.IsNullOrEmpty(musicPath))
        {
            musicPath = Path.Combine(Application.persistentDataPath, "Music", SelectedBeatMap.musicFileName);
        }

        AudioClip clip = null;
        bool loaded = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.LoadAudioClip(musicPath, (loadedClip) =>
            {
                clip = loadedClip;
                loaded = true;
            });
        }

        // Attendre le chargement
        while (!loaded)
            yield return null;

        if (clip == null)
        {
            Debug.LogError("GameManager: Failed to load music!");
            yield break;
        }

        // Initialiser le BeatManager
        if (BeatManager.Instance != null)
            BeatManager.Instance.Initialize(SelectedBeatMap.bpm, SelectedBeatMap.songOffset);

        // Initialiser le SpawnManager
        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.Initialize(SelectedBeatMap);

        // Reset score
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetAll();

        // Attendre avant de commencer
        currentState = GameState.WaitingToStart;
        yield return new WaitForSeconds(startDelay);

        // GO !
        currentState = GameState.Playing;
        AudioManager.Instance.PlayMusic(clip);
        BeatManager.Instance.StartBeat();
    }

    /// <summary>
    /// Crée une beatmap de test pour le développement.
    /// </summary>
    private void LoadTestBeatMap()
    {
        SelectedBeatMap = new BeatMapData
        {
            songName = "Test Map",
            bpm = 120,
            songOffset = 0f,
            notes = new System.Collections.Generic.List<BeatNote>()
        };

        // Ajouter des notes de test
        for (int i = 0; i < 32; i++)
        {
            SelectedBeatMap.notes.Add(new BeatNote(
                beatTime: 4 + i * 2,
                spawnerIndex: i % 8,
                inputType: EnemyInputType.Any
            ));
        }

        if (BeatManager.Instance != null)
            BeatManager.Instance.Initialize(120f, 0f);

        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.Initialize(SelectedBeatMap);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetAll();

        currentState = GameState.Playing;
        BeatManager.Instance?.StartBeat();
    }

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        // Vérifier si la musique est finie
        if (AudioManager.Instance != null && !AudioManager.Instance.IsPlaying &&
            EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.AllNotesProcessed())
        {
            HandleVictory();
        }

        // Pause avec Escape
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Toggle pause/resume.
    /// </summary>
    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
            AudioManager.Instance?.PauseMusic();
            BeatManager.Instance?.SetPaused(true);
        }
        else if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
            AudioManager.Instance?.ResumeMusic();
            BeatManager.Instance?.SetPaused(false);
        }
    }

    private void HandlePlayerDeath()
    {
        currentState = GameState.GameOver;
        AudioManager.Instance?.StopMusic();
        BeatManager.Instance?.StopBeat();
        EnemySpawnManager.Instance?.StopSpawning();
    }

    private void HandleVictory()
    {
        currentState = GameState.Victory;
        BeatManager.Instance?.StopBeat();
        Debug.Log($"Victory! Score: {ScoreManager.Instance?.TotalScore}, Best Combo: {ScoreManager.Instance?.BestCombo}");
    }

    /// <summary>
    /// Recommence la partie.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Retour au menu principal.
    /// </summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
