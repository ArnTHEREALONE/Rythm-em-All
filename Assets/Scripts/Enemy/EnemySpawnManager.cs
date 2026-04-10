using UnityEngine;
using System.Collections.Generic;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    public List<EnemySpawner> spawners = new List<EnemySpawner>();

    public EnemyData enemyDataSimple;

    public EnemyData enemyDataLeft;

    public EnemyData enemyDataRight;

    public EnemyData enemyDataBoth;

    public EnemyData enemyDataSpam;

    public float lookAheadBeats = 4f;

    public float warningAheadBeats = 2f;

    public float calmBeforeStormDuration = 5f;

    [SerializeField] private int nextNoteIndex;
    [SerializeField] private int nextWarningIndex;
    [SerializeField] private int activeEnemyCount;

    private BeatMapData currentMap;
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private float lastSpawnTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize(BeatMapData map)
    {
        currentMap = map;
        nextNoteIndex = 0;
        nextWarningIndex = 0;
        activeEnemies.Clear();
        lastSpawnTime = Time.time;

        if (currentMap.notes != null)
        {
            currentMap.notes.Sort((a, b) => a.beatTime.CompareTo(b.beatTime));
        }
    }

    public void StopSpawning()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
    }

    private void Update()
    {
        if (currentMap == null || currentMap.notes == null) return;
        if (BeatManager.Instance == null || !BeatManager.Instance.IsRunning) return;

        float currentBeat = BeatManager.Instance.CurrentBeat;

        ProcessWarnings(currentBeat);

        ProcessSpawns(currentBeat);

        CleanupDeadEnemies();

        activeEnemyCount = activeEnemies.Count;
    }

    private void ProcessWarnings(float currentBeat)
    {
        while (nextWarningIndex < currentMap.notes.Count)
        {
            BeatNote note = currentMap.notes[nextWarningIndex];
            float warningBeat = note.beatTime - warningAheadBeats;

            if (currentBeat >= warningBeat)
            {
                if (note.spawnerIndex >= 0 && note.spawnerIndex < spawners.Count)
                {
                    EnemyData data = GetEnemyDataForType(note.inputType);
                    if (data != null)
                    {
                        spawners[note.spawnerIndex].ShowWarning(data.warningColor);
                    }
                }
                nextWarningIndex++;
            }
            else
            {
                break;
            }
        }
    }

    private void ProcessSpawns(float currentBeat)
    {
        while (nextNoteIndex < currentMap.notes.Count)
        {
            BeatNote note = currentMap.notes[nextNoteIndex];
            float spawnBeat = note.beatTime - lookAheadBeats;

            if (currentBeat >= spawnBeat)
            {
                SpawnEnemyForNote(note);
                nextNoteIndex++;
                lastSpawnTime = Time.time;
            }
            else
            {
                break;
            }
        }
    }

    private void SpawnEnemyForNote(BeatNote note)
    {
        if (note.spawnerIndex < 0 || note.spawnerIndex >= spawners.Count)
        {
            Debug.LogWarning($"EnemySpawnManager: Invalid spawner index {note.spawnerIndex}");
            return;
        }

        EnemyData data = GetEnemyDataForType(note.inputType);
        if (data == null)
        {
            Debug.LogWarning($"EnemySpawnManager: No EnemyData for input type {note.inputType}");
            return;
        }

        EnemySpawner spawner = spawners[note.spawnerIndex];
        EnemyBase enemy = spawner.SpawnEnemy(note, data, note.beatTime);

        if (enemy != null)
        {
            enemy.OnEnemyKilled += HandleEnemyKilled;
            enemy.OnEnemyExploded += HandleEnemyExploded;
            activeEnemies.Add(enemy);
        }

        spawner.HideWarning();
    }

    private EnemyData GetEnemyDataForType(EnemyInputType inputType)
    {
        return inputType switch
        {
            EnemyInputType.Any => enemyDataSimple,
            EnemyInputType.LeftOnly => enemyDataLeft,
            EnemyInputType.RightOnly => enemyDataRight,
            EnemyInputType.Both => enemyDataBoth,
            EnemyInputType.Spam => enemyDataSpam,
            _ => enemyDataSimple
        };
    }

    private void HandleEnemyKilled(EnemyBase enemy, TimingResult result)
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterHit(result);
        }

        if (TimingFeedbackUI.Instance != null)
        {
            TimingFeedbackUI.Instance.ShowFeedback(result, enemy.transform.position);
        }
    }

    private void HandleEnemyExploded(EnemyBase enemy)
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterMiss();
        }

        if (TimingFeedbackUI.Instance != null)
        {
            TimingFeedbackUI.Instance.ShowFeedback(TimingResult.Miss, enemy.transform.position);
        }

    }
    private void CleanupDeadEnemies()
    {
        activeEnemies.RemoveAll(e => e == null);
    }

    public EnemyBase GetClosestVulnerableEnemy(Vector3 playerPosition, EnemyInputType inputType)
    {
        EnemyBase closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;
            if (enemy.CurrentState != EnemyBase.EnemyState.Vulnerable &&
                enemy.CurrentState != EnemyBase.EnemyState.Moving) continue;

            if (enemy.RequiredInput != EnemyInputType.Any &&
                enemy.RequiredInput != inputType &&
                inputType != EnemyInputType.Any) continue;

            float dist = Vector3.Distance(playerPosition, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    public bool AllNotesProcessed()
    {
        return currentMap != null &&
               nextNoteIndex >= currentMap.notes.Count &&
               activeEnemies.Count == 0;
    }
}
