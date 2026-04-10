using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Gestionnaire principal du gameplay. Gère le flow d'une partie.
/// La musique est jouée via FMOD — plus de chargement de fichier.
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
    public MusicDatabase musicDatabase;

    [Header("=== État ===")]
    [SerializeField] private GameState currentState = GameState.Loading;

    [Header("=== Délai ===")]
    [Tooltip("Délai avant le début de la musique (secondes)")]
    public float startDelay = 3f;

    /// <summary>État actuel du jeu.</summary>
    public GameState CurrentState => currentState;

    /// <summary>BeatMap actuellement chargée (définie avant le chargement de scène).</summary>
    public static BeatMapData SelectedBeatMap { get; set; }

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

        // Synchroniser le pitch avec le SpeedMultiplier
        if (SpeedMultiplier.Instance != null)
            SpeedMultiplier.Instance.OnSpeedChanged += OnSpeedChanged;

        // Charger la beatmap
        if (SelectedBeatMap != null)
        {
            StartCoroutine(StartGame());
        }
        else
        {
            Debug.LogWarning("GameManager: No beatmap selected! Loading test map...");
            LoadTestBeatMap();
        }
    }

    /// <summary>
    /// Démarre la partie avec la beatmap sélectionnée.
    /// Plus de chargement de fichier — FMOD event joue directement.
    /// </summary>
    private IEnumerator StartGame()
    {
        currentState = GameState.Loading;

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

        // GO ! — Jouer la musique via FMOD (l'event path est dans la beatmap)
        currentState = GameState.Playing;

        if (FMODAudioManager.Instance != null)
        {
            // Chercher le MusicEntry dans la base de données pour l'EventReference
            MusicEntry entry = musicDatabase?.GetByEventPath(SelectedBeatMap.fmodEventPath);
            if (entry != null)
            {
                FMODAudioManager.Instance.PlayMusic(entry.fmodEvent);
            }
            else
            {
                // Fallback : utiliser le chemin directement
                FMODAudioManager.Instance.PlayMusic(SelectedBeatMap.fmodEventPath);
            }

            // Écouter la fin de la musique
            FMODAudioManager.Instance.OnMusicEnded += CheckVictory;
        }

        BeatManager.Instance?.StartBeat();
    }

    /// <summary>
    /// Crée une beatmap de test pour le développement.
    /// </summary>
    private void LoadTestBeatMap()
    {
        SelectedBeatMap = new BeatMapData
        {
            songName = "Test Map",
            fmodEventPath = "event:/Music/Test",
            bpm = 120,
            songOffset = 0f,
            notes = new System.Collections.Generic.List<BeatNote>()
        };

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

    /// <summary>
    /// Callback quand le SpeedMultiplier change — ajuste le pitch FMOD.
    /// </summary>
    private void OnSpeedChanged(float newSpeed)
    {
        if (FMODAudioManager.Instance != null && currentState == GameState.Playing)
        {
            FMODAudioManager.Instance.SetPitch(newSpeed);
        }
    }

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        // Pause avec Escape
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void CheckVictory()
    {
        if (EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.AllNotesProcessed())
        {
            HandleVictory();
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
            FMODAudioManager.Instance?.PauseMusic();
            BeatManager.Instance?.SetPaused(true);
        }
        else if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
            FMODAudioManager.Instance?.ResumeMusic();
            BeatManager.Instance?.SetPaused(false);
        }
    }

    private void HandlePlayerDeath()
    {
        currentState = GameState.GameOver;
        FMODAudioManager.Instance?.StopMusic();
        BeatManager.Instance?.StopBeat();
        EnemySpawnManager.Instance?.StopSpawning();
    }

    private void HandleVictory()
    {
        currentState = GameState.Victory;
        BeatManager.Instance?.StopBeat();
        Debug.Log($"Victory! Score: {ScoreManager.Instance?.TotalScore}, Best Combo: {ScoreManager.Instance?.BestCombo}");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        FMODAudioManager.Instance?.StopMusic();
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (SpeedMultiplier.Instance != null)
            SpeedMultiplier.Instance.OnSpeedChanged -= OnSpeedChanged;
        if (FMODAudioManager.Instance != null)
            FMODAudioManager.Instance.OnMusicEnded -= CheckVictory;
    }
}
