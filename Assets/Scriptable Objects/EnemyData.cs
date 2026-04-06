using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyId;
    public GameObject prefab;
    public float hitPoints, speed;
    public int damage;
}
