using UnityEngine;

/// <summary>
/// Un spawner individuel positionné sur un côté de l'arène.
/// Chaque spawner = une lane dans l'éditeur de beatmap.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Index unique de ce spawner (correspond à BeatNote.spawnerIndex)")]
    public int spawnerIndex;

    [Tooltip("Direction dans laquelle les ennemis se déplacent")]
    public Vector3 spawnDirection = Vector3.forward;

    [Header("=== Warning ===")]
    [Tooltip("L'indicateur de warning attaché à ce spawner")]
    public WarningIndicator warningIndicator;

    [Tooltip("Durée du warning avant le spawn (en secondes)")]
    public float warningDuration = 2f;

    /// <summary>
    /// Spawn un ennemi depuis ce spawner.
    /// </summary>
    /// <param name="note">La note de la beatmap</param>
    /// <param name="data">Les données du type d'ennemi</param>
    /// <param name="targetBeat">Le beat auquel l'ennemi doit être vulnérable</param>
    /// <returns>L'instance de l'ennemi créé</returns>
    public EnemyBase SpawnEnemy(BeatNote note, EnemyData data, float targetBeat)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogError($"EnemySpawner {spawnerIndex}: Missing EnemyData or prefab!");
            return null;
        }

        // Instancier l'ennemi à la position du spawner
        GameObject enemyGO = Instantiate(data.prefab, transform.position, Quaternion.identity);
        enemyGO.name = $"Enemy_{data.enemyId}_{targetBeat:F1}";

        // Initialiser l'ennemi
        EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.Initialize(data, targetBeat);
        }

        // Initialiser le mouvement
        EnemyMovement movement = enemyGO.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.Initialize(spawnDirection, data.baseMoveSpeed);
        }

        return enemy;
    }

    /// <summary>
    /// Affiche le warning (ennemi va bientôt spawn).
    /// </summary>
    public void ShowWarning(Color color)
    {
        if (warningIndicator != null)
        {
            warningIndicator.Show(color, warningDuration);
        }
    }

    /// <summary>
    /// Cache le warning.
    /// </summary>
    public void HideWarning()
    {
        if (warningIndicator != null)
        {
            warningIndicator.Hide();
        }
    }

    private void OnDrawGizmos()
    {
        // Dessine la direction du spawner dans l'éditeur
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.3f);
        Gizmos.DrawRay(transform.position, spawnDirection * 3f);
    }
}
