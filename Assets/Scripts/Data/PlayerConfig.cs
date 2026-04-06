using UnityEngine;

/// <summary>
/// Configuration du joueur. Tous les champs sont paramétrables dans l'Inspector Unity.
/// Les vitesses et cooldowns sont multipliés/divisés par le gameSpeed en temps réel.
/// </summary>
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Scriptable Objects/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [Header("=== MOUVEMENT ===")]
    [Tooltip("Vitesse de déplacement de base (multipliée par gameSpeed en jeu)")]
    public float moveSpeed = 8f;

    [Header("=== DASH ===")]
    [Tooltip("Distance fixe parcourue lors d'un dash")]
    public float dashDistance = 5f;

    [Tooltip("Cooldown de base du dash en secondes (divisé par gameSpeed en jeu)")]
    public float dashCooldown = 1f;

    [Header("=== POINTS DE VIE ===")]
    [Tooltip("Points de vie maximum du joueur")]
    public float maxHP = 100f;

    [Tooltip("Soin instantané reçu par kill")]
    public float healOnKill = 5f;

    [Tooltip("PV/sec du soin progressif")]
    public float passiveHealRate = 1f;

    [Tooltip("Délai en secondes avant que le soin passif commence (après le dernier dégât reçu)")]
    public float passiveHealDelay = 3f;

    [Tooltip("Dégâts reçus quand un ennemi explose (miss)")]
    public int damageOnMiss = 10;

    [Tooltip("Dégâts réduits si le joueur frappe trop tôt")]
    public int damageOnTooSoon = 5;
}
