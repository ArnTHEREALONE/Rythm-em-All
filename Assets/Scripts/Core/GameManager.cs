using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Loading,
        Countdown,
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

    [Header("=== Volume écran de fin ===")]
    [Tooltip("Volume de la musique pendant les écrans de victoire/défaite (0-1)")]
    public float endScreenMusicVolume = 0.2f;

    [Header("=== Décompte ===")]
    [Tooltip("Son joué à chaque tick du décompte (3, 2, 1)")]
    public FMODUnity.EventReference countdownTickSFX;
    [Tooltip("Son joué au 'Go!'")]
    public FMODUnity.EventReference countdownGoSFX;

    public GameState CurrentState => currentState;

    public static BeatMapData SelectedBeatMap { get; set; }

    // Events
    public event Action<string> OnCountdownTick;
    public event Action OnGameStarted;
    public event Action<int, bool> OnVictory;
    public event Action<int> OnGameOver;

    private bool victoryTriggered = false;

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
        if (SpeedMultiplier.Instance != null)
            SpeedMultiplier.Instance.config = gameConfig;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.config = gameConfig;

        if (TimingJudge.Instance != null)
            TimingJudge.Instance.config = gameConfig;

        if (player != null && playerConfig != null)
            player.Initialize(playerConfig);

        if (player != null && player.health != null)
            player.health.OnDeath += HandlePlayerDeath;

        if (SpeedMultiplier.Instance != null)
            SpeedMultiplier.Instance.OnSpeedChanged += OnSpeedChanged;

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
    /// Lance le jeu : initialise les systèmes, lance la musique, affiche le décompte
    /// pendant le songOffset, puis démarre les beats.
    /// </summary>
    private IEnumerator StartGame()
    {
        currentState = GameState.Loading;
        victoryTriggered = false;

        // Initialiser BeatManager
        if (BeatManager.Instance != null)
            BeatManager.Instance.Initialize(SelectedBeatMap.bpm, SelectedBeatMap.songOffset);

        // Initialiser EnemySpawnManager
        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.Initialize(SelectedBeatMap);

        // Reset score
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetAll();

        // Lancer la musique FMOD immédiatement
        if (FMODAudioManager.Instance != null)
        {
            MusicEntry entry = musicDatabase?.GetByEventPath(SelectedBeatMap.fmodEventPath);
            if (entry != null)
                FMODAudioManager.Instance.PlayMusic(entry.fmodEvent);
            else
                FMODAudioManager.Instance.PlayMusic(SelectedBeatMap.fmodEventPath);

            FMODAudioManager.Instance.OnMusicEnded += CheckVictory;
        }

        // ── Décompte distribué sur le songOffset ──
        float offset = Mathf.Max(SelectedBeatMap.songOffset, 0.5f);
        currentState = GameState.Countdown;

        // On distribue 4 ticks (3, 2, 1, Go!) de façon uniforme sur offset
        float tickInterval = offset / 4f;
        string[] ticks = { "3", "2", "1", "Go!" };

        for (int i = 0; i < ticks.Length; i++)
        {
            OnCountdownTick?.Invoke(ticks[i]);

            if (i < ticks.Length - 1)
            {
                if (!countdownTickSFX.IsNull && FMODAudioManager.Instance != null)
                    FMODAudioManager.Instance.PlaySFX(countdownTickSFX);
            }
            else
            {
                if (!countdownGoSFX.IsNull && FMODAudioManager.Instance != null)
                    FMODAudioManager.Instance.PlaySFX(countdownGoSFX);
            }

            yield return new WaitForSeconds(tickInterval);
        }

        // ── Début du gameplay ──
        currentState = GameState.Playing;
        BeatManager.Instance?.StartBeat();
        OnGameStarted?.Invoke();
    }

    private void LoadTestBeatMap()
    {
        SelectedBeatMap = new BeatMapData
        {
            songName = "Test Map",
            fmodEventPath = "event:/Music/Test",
            bpm = 120,
            songOffset = 3f,
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
            BeatManager.Instance.Initialize(120f, 3f);

        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.Initialize(SelectedBeatMap);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetAll();

        currentState = GameState.Playing;
        BeatManager.Instance?.StartBeat();
    }

    private void OnSpeedChanged(float newSpeed)
    {
        if (FMODAudioManager.Instance != null && currentState == GameState.Playing)
        {
            FMODAudioManager.Instance.SetPitch(newSpeed);
        }
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            // Vérifier le marqueur de fin explicite
            if (SelectedBeatMap != null && SelectedBeatMap.endBeat > 0f && !victoryTriggered)
            {
                if (BeatManager.Instance != null && BeatManager.Instance.CurrentBeat >= SelectedBeatMap.endBeat)
                {
                    HandleVictory();
                    return;
                }
            }

            // Pause avec Echap
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
    }

    private void CheckVictory()
    {
        if (victoryTriggered) return;

        // Ne déclenche la victoire via OnMusicEnded que si pas de endBeat explicite
        if (SelectedBeatMap == null || SelectedBeatMap.endBeat <= 0f)
        {
            if (EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.AllNotesProcessed())
            {
                HandleVictory();
            }
        }
    }

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
        if (currentState == GameState.GameOver || currentState == GameState.Victory) return;

        currentState = GameState.GameOver;
        BeatManager.Instance?.StopBeat();
        EnemySpawnManager.Instance?.StopSpawning();

        // Baisser la musique sur l'écran de défaite
        FMODAudioManager.Instance?.SetMusicVolume(endScreenMusicVolume);

        int score = ScoreManager.Instance?.TotalScore ?? 0;
        OnGameOver?.Invoke(score);
    }

    private void HandleVictory()
    {
        if (victoryTriggered) return;
        victoryTriggered = true;

        currentState = GameState.Victory;
        BeatManager.Instance?.StopBeat();

        // Baisser la musique sur l'écran de victoire
        FMODAudioManager.Instance?.SetMusicVolume(endScreenMusicVolume);

        int score = ScoreManager.Instance?.TotalScore ?? 0;

        bool isNewRecord = false;
        if (SelectedBeatMap != null && ScoreManager.Instance != null)
        {
            int previousHS = MapSelectUI.GetHighScore(SelectedBeatMap.mapName);
            isNewRecord = score > previousHS;
            MapSelectUI.SaveHighScore(SelectedBeatMap.mapName, score);
        }

        Debug.Log($"Victory! Score: {score}, New Record: {isNewRecord}");
        OnVictory?.Invoke(score, isNewRecord);
    }

    public void RestartGame()
    {
        // Restaurer le volume avant de relancer
        FMODAudioManager.Instance?.SetMusicVolume(1f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        FMODAudioManager.Instance?.SetMusicVolume(1f);
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
