using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Orchestre le spawn de tous les ennemis selon la beatmap.
/// Gère le look-ahead, les warnings, et le pool d'ennemis actifs.
/// </summary>
public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    [Header("=== Références ===")]
    [Tooltip("Liste de tous les spawners dans la scène")]
    public List<EnemySpawner> spawners = new List<EnemySpawner>();

    [Header("=== EnemyData par type d'input ===")]
    [Tooltip("Données pour l'ennemi simple (Any/Space)")]
    public EnemyData enemyDataSimple;

    [Tooltip("Données pour l'ennemi gauche")]
    public EnemyData enemyDataLeft;

    [Tooltip("Données pour l'ennemi droite")]
    public EnemyData enemyDataRight;

    [Tooltip("Données pour l'ennemi both")]
    public EnemyData enemyDataBoth;

    [Tooltip("Données pour l'ennemi spam")]
    public EnemyData enemyDataSpam;

    [Header("=== Configuration ===")]
    [Tooltip("Combien de beats à l'avance on prépare les spawns")]
    public float lookAheadBeats = 4f;

    [Tooltip("Combien de beats à l'avance on affiche le warning")]
    public float warningAheadBeats = 2f;

    [Tooltip("Durée sans spawn avant d'afficher un warning spécial (secondes)")]
    public float calmBeforeStormDuration = 5f;

    [Header("=== État (debug) ===")]
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

    /// <summary>
    /// Initialise le manager avec une beatmap.
    /// </summary>
    public void Initialize(BeatMapData map)
    {
        currentMap = map;
        nextNoteIndex = 0;
        nextWarningIndex = 0;
        activeEnemies.Clear();
        lastSpawnTime = Time.time;

        // Trier les notes par beatTime
        if (currentMap.notes != null)
        {
            currentMap.notes.Sort((a, b) => a.beatTime.CompareTo(b.beatTime));
        }
    }

    /// <summary>
    /// Reset et arrête le spawn.
    /// </summary>
    public void StopSpawning()
    {
        // Détruire tous les ennemis actifs
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

        // === Gestion des warnings ===
        ProcessWarnings(currentBeat);

        // === Gestion des spawns ===
        ProcessSpawns(currentBeat);

        // === Nettoyage des ennemis morts ===
        CleanupDeadEnemies();

        activeEnemyCount = activeEnemies.Count;
    }

    /// <summary>
    /// Affiche les warnings pour les ennemis qui vont bientôt spawn.
    /// </summary>
    private void ProcessWarnings(float currentBeat)
    {
        while (nextWarningIndex < currentMap.notes.Count)
        {
            BeatNote note = currentMap.notes[nextWarningIndex];
            float warningBeat = note.beatTime - warningAheadBeats;

            if (currentBeat >= warningBeat)
            {
                // Afficher le warning sur le spawner correspondant
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
                break; // Les notes suivantes sont encore plus loin
            }
        }
    }

    /// <summary>
    /// Spawn les ennemis dont le moment est venu.
    /// </summary>
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

    /// <summary>
    /// Spawn un ennemi pour une note donnée.
    /// </summary>
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
            // S'abonner aux events de l'ennemi
            enemy.OnEnemyKilled += HandleEnemyKilled;
            enemy.OnEnemyExploded += HandleEnemyExploded;
            activeEnemies.Add(enemy);
        }

        // Cacher le warning
        spawner.HideWarning();
    }

    /// <summary>
    /// Retourne le bon EnemyData selon le type d'input.
    /// </summary>
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
    /// Callback quand un ennemi est tué par le joueur.
    /// </summary>
    private void HandleEnemyKilled(EnemyBase enemy, TimingResult result)
    {
        // Le ScoreManager gère le score et le multiplicateur
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterHit(result);
        }

        // Feedback visuel
        if (TimingFeedbackUI.Instance != null)
        {
            TimingFeedbackUI.Instance.ShowFeedback(result, enemy.transform.position);
        }
    }

    /// <summary>
    /// Callback quand un ennemi explose (pas frappé à temps).
    /// </summary>
    private void HandleEnemyExploded(EnemyBase enemy)
    {
        // Miss !
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterMiss();
        }

        // Feedback visuel
        if (TimingFeedbackUI.Instance != null)
        {
            TimingFeedbackUI.Instance.ShowFeedback(TimingResult.Miss, enemy.transform.position);
        }

        // Dégâts au joueur (géré par PlayerHealth qui écoute cet event)
    }

    /// <summary>
    /// Retire les ennemis morts de la liste active.
    /// </summary>
    private void CleanupDeadEnemies()
    {
        activeEnemies.RemoveAll(e => e == null);
    }

    /// <summary>
    /// Retourne l'ennemi vulnérable le plus proche du joueur.
    /// Utilisé par PlayerCombat pour cibler automatiquement.
    /// </summary>
    public EnemyBase GetClosestVulnerableEnemy(Vector3 playerPosition, EnemyInputType inputType)
    {
        EnemyBase closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;
            if (enemy.CurrentState != EnemyBase.EnemyState.Vulnerable &&
                enemy.CurrentState != EnemyBase.EnemyState.Moving) continue;

            // Vérifie le type d'input
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

    /// <summary>
    /// Vérifie si tous les ennemis de la beatmap ont été traités.
    /// </summary>
    public bool AllNotesProcessed()
    {
        return currentMap != null &&
               nextNoteIndex >= currentMap.notes.Count &&
               activeEnemies.Count == 0;
    }
}
