using UnityEngine;

/// <summary>
/// Handles death visual effects for an enemy prefab.
/// Spawns particle systems on death and triggers arena-wide effects on auto-destruct.
/// Attach to the same GameObject as EnemyBase.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyDeathVFX : MonoBehaviour
{
    [Header("=== Death by Player ===")]
    [Tooltip("ParticleSystem prefab to instantiate on death by player (create manually in Unity).")]
    public ParticleSystem deathByPlayerParticlesPrefab;

    [Header("=== Auto-Destruct ===")]
    [Tooltip("ParticleSystem prefab to instantiate on auto-destruct.")]
    public ParticleSystem autoDestructParticlesPrefab;

    private EnemyBase enemyBase;

    private void Awake()
    {
        enemyBase = GetComponent<EnemyBase>();
    }

    private void OnEnable()
    {
        if (enemyBase != null)
        {
            enemyBase.OnEnemyKilled += HandleEnemyKilled;
            enemyBase.OnEnemyExploded += HandleEnemyExploded;
        }
    }

    private void OnDisable()
    {
        if (enemyBase != null)
        {
            enemyBase.OnEnemyKilled -= HandleEnemyKilled;
            enemyBase.OnEnemyExploded -= HandleEnemyExploded;
        }
    }

    /// <summary>
    /// Called when the enemy is killed by the player.
    /// Spawns death particles colored to match the enemy and shows timing feedback.
    /// </summary>
    private void HandleEnemyKilled(EnemyBase enemy, TimingResult result)
    {
        Vector3 spawnPos = enemy.transform.position;

        // Spawn death particles
        if (deathByPlayerParticlesPrefab != null)
        {
            ParticleSystem ps = Instantiate(deathByPlayerParticlesPrefab, spawnPos, Quaternion.identity);

            // Tint particles to match the enemy's renderer color
            if (enemy.mainRenderer != null)
            {
                var main = ps.main;
                main.startColor = enemy.mainRenderer.material.color;
            }
        }

        // Show timing feedback
        if (TimingFeedbackUI.Instance != null)
        {
            TimingFeedbackUI.Instance.ShowFeedback(result, spawnPos);
        }
    }

    /// <summary>
    /// Called when the enemy auto-destructs (explodes).
    /// Spawns explosion particles and triggers arena-wide punishment effects.
    /// </summary>
    private void HandleEnemyExploded(EnemyBase enemy)
    {
        Vector3 spawnPos = enemy.transform.position;

        // Spawn auto-destruct particles
        if (autoDestructParticlesPrefab != null)
        {
            Instantiate(autoDestructParticlesPrefab, spawnPos, Quaternion.identity);
        }

        // Arena-wide punishment effects
        if (ArenaVFX.Instance != null)
        {
            ArenaVFX.Instance.FlashScreenRed(0.3f);
            ArenaVFX.Instance.FlashDamierRed(0.3f);
            ArenaVFX.Instance.GlitchEffect(0.2f, 0.1f);
        }

        // Show 'Too Late' feedback
        if (TimingFeedbackUI.Instance != null)
        {
            TimingFeedbackUI.Instance.ShowFeedback(TimingResult.TooLate, spawnPos);
        }
    }
}
