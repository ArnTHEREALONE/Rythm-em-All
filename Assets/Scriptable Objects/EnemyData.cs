using UnityEngine;

/// <summary>
/// Données d'un type d'ennemi. Chaque variante (Simple, Left, Right, Both, Spam)
/// a son propre asset ScriptableObject avec des valeurs personnalisées.
/// La vitesse est une valeur de base qui sera multipliée par le gameSpeed en jeu.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("=== IDENTITÉ ===")]
    [Tooltip("Identifiant unique de ce type d'ennemi")]
    public string enemyId;

    [Tooltip("Prefab à instancier pour cet ennemi")]
    public GameObject prefab;

    [Header("=== STATS ===")]
    [Tooltip("Nombre de clics/hits nécessaires pour tuer cet ennemi")]
    public int hitPoints = 1;

    [Tooltip("Vitesse de déplacement de base (multipliée par gameSpeed en jeu)")]
    public float baseMoveSpeed = 3f;

    [Tooltip("Dégâts infligés au joueur si l'ennemi explose")]
    public int damage = 10;

    [Header("=== INPUT ===")]
    [Tooltip("Type d'input requis pour frapper cet ennemi")]
    public EnemyInputType requiredInput = EnemyInputType.Any;

    [Header("=== VISUEL ===")]
    [Tooltip("Couleur de l'indicateur de warning pour cet ennemi")]
    public Color warningColor = Color.red;

    [Header("=== SPAM (si requiredInput = Spam) ===")]
    [Tooltip("Nombre de clics nécessaires pour tuer en mode Spam")]
    public int spamClicksRequired = 5;

    [Tooltip("Fenêtre de temps en secondes pour réussir le spam")]
    public float spamWindowDuration = 1f;
}
