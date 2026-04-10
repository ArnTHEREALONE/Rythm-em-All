using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyId;

    public GameObject prefab;

    public int hitPoints = 1;

    public float baseMoveSpeed = 3f;

    public int damage = 10;

    public EnemyInputType requiredInput = EnemyInputType.Any;

    public Color warningColor = Color.red;

    public int spamClicksRequired = 5;

    public float spamWindowDuration = 1f;
}
