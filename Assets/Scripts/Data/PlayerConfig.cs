using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Scriptable Objects/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [Header("=== Mouvement ===")]
    public float moveSpeed = 8f;

    [Header("=== Dash ===")]
    public float dashDistance = 5f;
    public float dashCooldown = 1f;

    [Header("=== PV ===")]
    public float maxHP = 100f;
    public float healOnKill = 5f;
    public float passiveHealRate = 1f;
    public float passiveHealDelay = 3f;

    [Header("=== Dégâts subis ===")]
    [Tooltip("Dégâts quand un ennemi explose (non tué par le joueur)")]
    public int damageOnMiss = 10;

    [Tooltip("Dégâts quand le joueur frappe trop tôt")]
    public int damageOnTooSoon = 5;

    [Tooltip("Dégâts quand le joueur frappe trop tard")]
    public int damageOnTooLate = 3;
}
