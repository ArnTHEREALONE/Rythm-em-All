using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public int spawnerIndex;

    public Vector3 spawnDirection = Vector3.forward;

    public WarningIndicator warningIndicator;

    public float warningDuration = 2f;

    public EnemyBase SpawnEnemy(BeatNote note, EnemyData data, float targetBeat)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogError($"EnemySpawner {spawnerIndex}: Missing EnemyData or prefab!");
            return null;
        }

        GameObject enemyGO = Instantiate(data.prefab, transform.position, Quaternion.identity);
        enemyGO.name = $"Enemy_{data.enemyId}_{targetBeat:F1}";

        EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.Initialize(data, targetBeat);
        }

        EnemyMovement movement = enemyGO.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.Initialize(spawnDirection, data.baseMoveSpeed);
        }

        return enemy;
    }

    public void ShowWarning(Color color)
    {
        if (warningIndicator != null)
        {
            warningIndicator.Show(color, warningDuration);
        }
    }

    public void HideWarning()
    {
        if (warningIndicator != null)
        {
            warningIndicator.Hide();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.3f);
        Gizmos.DrawRay(transform.position, spawnDirection * 3f);
    }
}
