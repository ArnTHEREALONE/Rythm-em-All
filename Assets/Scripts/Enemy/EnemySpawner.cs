using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public int spawnerIndex;

    public Vector3 spawnDirection = Vector3.forward;

    public WarningIndicator warningIndicator;

    public float warningDuration = 2f;

    public EnemyBase SpawnEnemy(BeatNote note, EnemyData data, float spawnBeat)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogError($"EnemySpawner {spawnerIndex}: Missing EnemyData or prefab!");
            return null;
        }

        GameObject enemyGO = Instantiate(data.prefab, transform.position, Quaternion.identity);
        enemyGO.name = $"Enemy_{data.enemyId}_spawn{spawnBeat:F1}";

        EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.Initialize(data, spawnBeat);
        }

        EnemyMovement movement = enemyGO.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.Initialize(spawnDirection, data.baseMoveSpeed);

            // Configurer le zigzag depuis l'EnemyData
            if (data.enableZigzag)
            {
                movement.SetZigzag(true, data.zigzagAmplitude, data.zigzagFrequency);
            }
        }

        // Ajouter le composant de contact damage si nécessaire
        if (data.damageOnContact)
        {
            var contact = enemyGO.GetComponent<EnemyContactDamage>();
            if (contact == null)
                contact = enemyGO.AddComponent<EnemyContactDamage>();

            int dmg = data.contactDamage > 0 ? data.contactDamage : data.damage;
            contact.Initialize(dmg);
        }

        return enemy;
    }

    public void ShowWarning(Color color, float duration = -1f)
    {
        if (warningIndicator != null)
        {
            float actualDuration = duration > 0 ? duration : warningDuration;
            warningIndicator.Show(color, actualDuration);
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
