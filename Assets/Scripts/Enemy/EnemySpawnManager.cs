using UnityEngine;
using System.Collections.Generic;





[System.Serializable]
public class EnemyWave
{
    public int waveIndex;
    public float startBeat;
    public float endBeat;
    public List<int> spawnerIndices = new List<int>();
    public List<BeatNote> notes = new List<BeatNote>();
}










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
    [Tooltip("[DÉPRÉCIÉ] Ce paramètre n'a plus d'effet sur le spawn.\n" +
             "La durée avant le timing de vulnérabilité est maintenant définie par\n" +
             "'Beats Before Vulnerable' sur chaque EnemyData (Scriptable Object).")]
    public float lookAheadBeats = 4f;

    [Header("=== Vagues ===")]
    [Tooltip("Durée minimum de pause (en secondes) pour séparer deux vagues")]
    public float waveGapSeconds = 5f;

    [Tooltip("Combien de beats avant le premier spawn d'une vague pour afficher les warnings")]
    public float warningAheadBeats = 6f;

    [Header("=== Référence Player ===")]
    [Tooltip("Glisser le PlayerHealth ici pour les dégâts d'explosion")]
    public PlayerHealth playerHealth;

    [Header("=== SFX (FMOD) ===")]
    [Tooltip("Son joué à l'apparition des warnings d'une nouvelle vague")]
    public FMODUnity.EventReference waveWarningSFX;

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

        
        if (playerHealth == null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                playerHealth = player.health;
        }

        
        DetectWaves();
    }

    
    
    
    
    private void DetectWaves()
    {
        waves.Clear();

        if (currentMap == null || currentMap.notes == null || currentMap.notes.Count == 0)
        {
            totalWaves = 0;
            return;
        }

        
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

            
            if (i > 0)
            {
                float gap = note.beatTime - currentMap.notes[i - 1].beatTime;
                if (gap >= waveGapBeats)
                {
                    
                    currentWave.endBeat = currentMap.notes[i - 1].beatTime;
                    waves.Add(currentWave);

                    
                    currentWave = new EnemyWave
                    {
                        waveIndex = waves.Count,
                        startBeat = note.beatTime,
                        spawnerIndices = new List<int>(),
                        notes = new List<BeatNote>()
                    };
                }
            }

            
            currentWave.notes.Add(note);
            if (!currentWave.spawnerIndices.Contains(note.spawnerIndex))
                currentWave.spawnerIndices.Add(note.spawnerIndex);
        }

        
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

    
    
    
    
    
    private void ProcessWaveWarnings(float currentBeat)
    {
        while (nextWaveWarningIndex < waves.Count)
        {
            EnemyWave wave = waves[nextWaveWarningIndex];
            float warningBeat = wave.startBeat - warningAheadBeats;

            if (currentBeat >= warningBeat)
            {
                
                foreach (int spawnerIdx in wave.spawnerIndices)
                {
                    if (spawnerIdx >= 0 && spawnerIdx < spawners.Count)
                    {
                        
                        Color warningColor = Color.red;
                        var firstNote = wave.notes.Find(n => n.spawnerIndex == spawnerIdx);
                        if (firstNote != null)
                        {
                            EnemyData data = GetEnemyDataForType(firstNote.inputType);
                            if (data != null)
                                warningColor = data.warningColor;
                        }

                        
                        float warningDuration = (wave.startBeat - lookAheadBeats - currentBeat)
                            * currentMap.SecondsPerBeat;
                        warningDuration = Mathf.Max(warningDuration, 1f); 

                        spawners[spawnerIdx].ShowWarning(warningColor, warningDuration);
                    }
                }

                if (!waveWarningSFX.IsNull && FMODAudioManager.Instance != null)
                {
                    FMODAudioManager.Instance.PlaySFX(waveWarningSFX);
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

            // On a besoin de l'EnemyData pour connaître beatsBeforeVulnerable
            // afin de calculer le bon beat de spawn.
            EnemyData data = GetEnemyDataForType(note.inputType);
            float beatsBeforeVulnerable = data != null ? data.beatsBeforeVulnerable : 4f;

            // Le spawn se déclenche AVANT le beat de la note :
            // spawnBeat = note.beatTime - beatsBeforeVulnerable
            // L'ennemi voyage pendant beatsBeforeVulnerable beats, puis devient vulnérable à note.beatTime.
            float spawnBeat = note.beatTime - beatsBeforeVulnerable;

            if (currentBeat >= spawnBeat)
            {
                SpawnEnemyForNote(note, data, spawnBeat);
                nextNoteIndex++;
            }
            else
            {
                break;
            }
        }
    }

    private void SpawnEnemyForNote(BeatNote note, EnemyData data, float spawnBeat)
    {
        if (note.spawnerIndex < 0 || note.spawnerIndex >= spawners.Count)
        {
            Debug.LogWarning($"EnemySpawnManager: Invalid spawner index {note.spawnerIndex}");
            return;
        }

        if (data == null)
        {
            Debug.LogWarning($"EnemySpawnManager: No EnemyData for input type {note.inputType}");
            return;
        }

        EnemySpawner spawner = spawners[note.spawnerIndex];
        // spawnBeat = note.beatTime - data.beatsBeforeVulnerable
        // EnemyBase.Initialize calculera targetBeatTime = spawnBeat + beatsBeforeVulnerable = note.beatTime
        EnemyBase enemy = spawner.SpawnEnemy(note, data, spawnBeat);

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

    
    
    
    private void HandleEnemyExploded(EnemyBase enemy)
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.RegisterMiss();

        if (TimingFeedbackUI.Instance != null)
            TimingFeedbackUI.Instance.ShowFeedback(TimingResult.Miss, enemy.transform.position);

        
        if (playerHealth != null && enemy.data != null)
        {
            playerHealth.TakeDamage(enemy.data.damage);
        }
    }

    private void CleanupDeadEnemies()
    {
        activeEnemies.RemoveAll(e => e == null);
    }

    
    
    
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
