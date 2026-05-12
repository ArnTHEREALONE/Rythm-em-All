using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;





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

    
    public GameState CurrentState => currentState;

    
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

    
    
    
    
    private IEnumerator StartGame()
    {
        currentState = GameState.Loading;

        
        if (BeatManager.Instance != null)
            BeatManager.Instance.Initialize(SelectedBeatMap.bpm, SelectedBeatMap.songOffset);

        
        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.Initialize(SelectedBeatMap);

        
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetAll();

        
        currentState = GameState.WaitingToStart;
        yield return new WaitForSeconds(startDelay);

        
        currentState = GameState.Playing;

        if (FMODAudioManager.Instance != null)
        {
            
            MusicEntry entry = musicDatabase?.GetByEventPath(SelectedBeatMap.fmodEventPath);
            if (entry != null)
            {
                FMODAudioManager.Instance.PlayMusic(entry.fmodEvent);
            }
            else
            {
                
                FMODAudioManager.Instance.PlayMusic(SelectedBeatMap.fmodEventPath);
            }

            
            FMODAudioManager.Instance.OnMusicEnded += CheckVictory;
        }

        BeatManager.Instance?.StartBeat();
    }

    
    
    
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
