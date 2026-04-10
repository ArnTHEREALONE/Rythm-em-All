using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Scriptable Objects/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    public float moveSpeed = 8f;

    public float dashDistance = 5f;

    public float dashCooldown = 1f;

    public float maxHP = 100f;

    public float healOnKill = 5f;

    public float passiveHealRate = 1f;

    public float passiveHealDelay = 3f;

    public int damageOnMiss = 10;

    public int damageOnTooSoon = 5;
}
