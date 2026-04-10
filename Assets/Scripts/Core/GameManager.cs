using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

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

    public GameConfig gameConfig;
    public PlayerConfig playerConfig;
    public PlayerController player;

    [SerializeField] private GameState currentState = GameState.Loading;

    public float startDelay = 3f;

    public GameState CurrentState => currentState;

    public static BeatMapData SelectedBeatMap { get; set; }

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

        if (SelectedBeatMap != null)
        {
            StartCoroutine(LoadAndStart());
        }
        else
        {
            Debug.LogWarning("GameManager: No beatmap selected! Loading test map...");
            LoadTestBeatMap();
        }
    }

    private IEnumerator LoadAndStart()
    {
        currentState = GameState.Loading;

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

        while (!loaded)
            yield return null;

        if (clip == null)
        {
            Debug.LogError("GameManager: Failed to load music!");
            yield break;
        }

        if (BeatManager.Instance != null)
            BeatManager.Instance.Initialize(SelectedBeatMap.bpm, SelectedBeatMap.songOffset);

        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.Initialize(SelectedBeatMap);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetAll();

        currentState = GameState.WaitingToStart;
        yield return new WaitForSeconds(startDelay);

        currentState = GameState.Playing;
        AudioManager.Instance.PlayMusic(clip);
        BeatManager.Instance.StartBeat();
    }

    private void LoadTestBeatMap()
    {
        SelectedBeatMap = new BeatMapData
        {
            songName = "Test Map",
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

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        if (AudioManager.Instance != null && !AudioManager.Instance.IsPlaying &&
            EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.AllNotesProcessed())
        {
            HandleVictory();
        }

        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

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

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
