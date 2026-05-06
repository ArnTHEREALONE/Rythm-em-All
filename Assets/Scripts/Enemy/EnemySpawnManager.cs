using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestionnaire des spawns d'ennemis. Lit la beatmap et spawn les ennemis.
/// Gère les warnings et le cleanup.
/// </summary>
public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    [Header("=== Spawners ===")]
    public List<EnemySpawner> spawners = new List<EnemySpawner>();

    [Header("=== Enemy Data (par type d'input) ===")]
    public EnemyData enemyDataSimple;
    public EnemyData enemyDataLeft;
    public EnemyData enemyDataRight;
    public EnemyData enemyDataBoth;
    public EnemyData enemyDataSpam;

    [Header("=== Timing ===")]
    [Tooltip("Combien de beats avant le targetBeat pour spawner l'ennemi")]
    public float lookAheadBeats = 4f;

    [Tooltip("Combien de beats avant le targetBeat pour afficher le warning")]
    public float warningAheadBeats = 2f;

    [Header("=== État (debug) ===")]
    [SerializeField] private int nextNoteIndex;
    [SerializeField] private int nextWarningIndex;
    [SerializeField] private int activeEnemyCount;

    private BeatMapData currentMap;
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();

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

    /// <summary>
    /// Quand un ennemi explose naturellement (pas tué par le joueur) = miss.
    /// </summary>
    private void HandleEnemyExploded(EnemyBase enemy)
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.RegisterMiss();

        if (TimingFeedbackUI.Instance != null)
            TimingFeedbackUI.Instance.ShowFeedback(TimingResult.Miss, enemy.transform.position);

        // Dégâts au joueur
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.health != null && enemy.data != null)
        {
            player.health.TakeDamage(enemy.data.damage);
        }
    }

    private void CleanupDeadEnemies()
    {
        activeEnemies.RemoveAll(e => e == null);
    }

    /// <summary>
    /// Trouve l'ennemi le plus proche du joueur, DANS N'IMPORTE QUEL ÉTAT (Moving ou Vulnerable).
    /// Le joueur peut frapper à tout moment.
    /// </summary>
    public EnemyBase GetClosestEnemy(Vector3 playerPosition, EnemyInputType inputType, float maxRange)
    {
        EnemyBase closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;
            if (enemy.CurrentState == EnemyBase.EnemyState.Dead ||
                enemy.CurrentState == EnemyBase.EnemyState.Exploding) continue;

            // Vérifier le type d'input
            if (enemy.RequiredInput != EnemyInputType.Any &&
                enemy.RequiredInput != inputType &&
                inputType != EnemyInputType.Any) continue;

            float dist = Vector3.Distance(playerPosition, enemy.transform.position);
            if (dist > maxRange) continue;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    /// <summary>
    /// Ancienne méthode pour compatibilité. Redirige vers GetClosestEnemy.
    /// </summary>
    public EnemyBase GetClosestVulnerableEnemy(Vector3 playerPosition, EnemyInputType inputType)
    {
        return GetClosestEnemy(playerPosition, inputType, 999f);
    }

    public bool AllNotesProcessed()
    {
        return currentMap != null &&
               nextNoteIndex >= currentMap.notes.Count &&
               activeEnemies.Count == 0;
    }
}
