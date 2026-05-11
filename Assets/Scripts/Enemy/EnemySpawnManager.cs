using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Structure pour une vague d'ennemis.
/// Une vague = un groupe de notes consécutives sans pause ≥ waveGapSeconds entre elles.
/// </summary>
[System.Serializable]
public class EnemyWave
{
    public int waveIndex;
    public float startBeat;
    public float endBeat;
    public List<int> spawnerIndices = new List<int>();
    public List<BeatNote> notes = new List<BeatNote>();
}

/// <summary>
/// Gestionnaire des spawns d'ennemis. Lit la beatmap et spawn les ennemis.
/// Gère les warnings par VAGUE et le cleanup.
///
/// SYSTÈME DE VAGUES :
/// 1. À l'initialisation, les notes sont groupées en vagues.
/// 2. Un gap ≥ waveGapSeconds entre deux notes consécutives = nouvelle vague.
/// 3. Les warnings s'affichent au début de chaque vague (sur tous les spawners de la vague).
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

    [Header("=== Vagues ===")]
    [Tooltip("Durée minimum de pause (en secondes) pour séparer deux vagues")]
    public float waveGapSeconds = 5f;

    [Tooltip("Combien de beats avant le premier spawn d'une vague pour afficher les warnings")]
    public float warningAheadBeats = 6f;

    [Header("=== Référence Player ===")]
    [Tooltip("Glisser le PlayerHealth ici pour les dégâts d'explosion")]
    public PlayerHealth playerHealth;

    [Header("=== État (debug) ===")]
    [SerializeField] private int nextNoteIndex;
    [SerializeField] private int nextWaveWarningIndex;
    [SerializeField] private int activeEnemyCount;
    [SerializeField] private int totalWaves;

    private BeatMapData currentMap;
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private List<EnemyWave> waves = new List<EnemyWave>();

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
        nextWaveWarningIndex = 0;
        activeEnemies.Clear();

        if (currentMap.notes != null)
        {
            currentMap.notes.Sort((a, b) => a.beatTime.CompareTo(b.beatTime));
        }

        // Auto-trouver le PlayerHealth si pas branché
        if (playerHealth == null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                playerHealth = player.health;
        }

        // Détecter les vagues
        DetectWaves();
    }

    /// <summary>
    /// Détecte les vagues en analysant les gaps entre notes consécutives.
    /// Un gap ≥ waveGapSeconds (converti en beats) = nouvelle vague.
    /// </summary>
    private void DetectWaves()
    {
        waves.Clear();

        if (currentMap == null || currentMap.notes == null || currentMap.notes.Count == 0)
        {
            totalWaves = 0;
            return;
        }

        // Convertir le gap en beats
        float waveGapBeats = waveGapSeconds / currentMap.SecondsPerBeat;

        EnemyWave currentWave = new EnemyWave
        {
            waveIndex = 0,
            startBeat = currentMap.notes[0].beatTime,
            spawnerIndices = new List<int>(),
            notes = new List<BeatNote>()
        };

        for (int i = 0; i < currentMap.notes.Count; i++)
        {
            BeatNote note = currentMap.notes[i];

            // Si c'est pas la première note et qu'il y a un gap
            if (i > 0)
            {
                float gap = note.beatTime - currentMap.notes[i - 1].beatTime;
                if (gap >= waveGapBeats)
                {
                    // Fermer la vague actuelle
                    currentWave.endBeat = currentMap.notes[i - 1].beatTime;
                    waves.Add(currentWave);

                    // Nouvelle vague
                    currentWave = new EnemyWave
                    {
                        waveIndex = waves.Count,
                        startBeat = note.beatTime,
                        spawnerIndices = new List<int>(),
                        notes = new List<BeatNote>()
                    };
                }
            }

            // Ajouter la note à la vague courante
            currentWave.notes.Add(note);
            if (!currentWave.spawnerIndices.Contains(note.spawnerIndex))
                currentWave.spawnerIndices.Add(note.spawnerIndex);
        }

        // Fermer la dernière vague
        currentWave.endBeat = currentMap.notes[currentMap.notes.Count - 1].beatTime;
        waves.Add(currentWave);

        totalWaves = waves.Count;
        Debug.Log($"EnemySpawnManager: {totalWaves} vagues détectées (gap = {waveGapSeconds}s = {waveGapBeats:F1} beats)");

        for (int w = 0; w < waves.Count; w++)
        {
            var wave = waves[w];
            Debug.Log($"  Vague {w + 1}: beats {wave.startBeat:F1} → {wave.endBeat:F1}, " +
                $"{wave.notes.Count} notes, spawners: [{string.Join(", ", wave.spawnerIndices)}]");
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

        ProcessWaveWarnings(currentBeat);
        ProcessSpawns(currentBeat);
        CleanupDeadEnemies();

        activeEnemyCount = activeEnemies.Count;
    }

    /// <summary>
    /// Affiche les warnings au début de chaque VAGUE.
    /// Quand le beat courant atteint (wave.startBeat - warningAheadBeats),
    /// les warnings s'affichent sur TOUS les spawners de la vague.
    /// </summary>
    private void ProcessWaveWarnings(float currentBeat)
    {
        while (nextWaveWarningIndex < waves.Count)
        {
            EnemyWave wave = waves[nextWaveWarningIndex];
            float warningBeat = wave.startBeat - warningAheadBeats;

            if (currentBeat >= warningBeat)
            {
                // Afficher le warning sur CHAQUE spawner de cette vague
                foreach (int spawnerIdx in wave.spawnerIndices)
                {
                    if (spawnerIdx >= 0 && spawnerIdx < spawners.Count)
                    {
                        // Déterminer la couleur (prendre le type dominant de la vague)
                        Color warningColor = Color.red;
                        var firstNote = wave.notes.Find(n => n.spawnerIndex == spawnerIdx);
                        if (firstNote != null)
                        {
                            EnemyData data = GetEnemyDataForType(firstNote.inputType);
                            if (data != null)
                                warningColor = data.warningColor;
                        }

                        // Durée du warning = temps entre maintenant et le premier spawn
                        float warningDuration = (wave.startBeat - lookAheadBeats - currentBeat)
                            * currentMap.SecondsPerBeat;
                        warningDuration = Mathf.Max(warningDuration, 1f); // au moins 1 seconde

                        spawners[spawnerIdx].ShowWarning(warningColor, warningDuration);
                    }
                }

                Debug.Log($"EnemySpawnManager: ⚠ Warning vague {wave.waveIndex + 1} ! " +
                    $"({wave.notes.Count} ennemis sur spawners [{string.Join(", ", wave.spawnerIndices)}])");

                nextWaveWarningIndex++;
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
    /// Quand un ennemi explose naturellement (pas tué par le joueur) = miss + dégâts.
    /// </summary>
    private void HandleEnemyExploded(EnemyBase enemy)
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.RegisterMiss();

        if (TimingFeedbackUI.Instance != null)
            TimingFeedbackUI.Instance.ShowFeedback(TimingResult.Miss, enemy.transform.position);

        // Dégâts au joueur (via la ref directe)
        if (playerHealth != null && enemy.data != null)
        {
            playerHealth.TakeDamage(enemy.data.damage);
        }
    }

    private void CleanupDeadEnemies()
    {
        activeEnemies.RemoveAll(e => e == null);
    }

    /// <summary>
    /// Trouve l'ennemi le plus proche (Moving ou Vulnerable).
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
