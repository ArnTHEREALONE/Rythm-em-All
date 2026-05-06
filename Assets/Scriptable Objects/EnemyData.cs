using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("=== Identité ===")]
    public string enemyId;
    public GameObject prefab;

    [Header("=== Stats ===")]
    [Tooltip("Points de vie de l'ennemi (1 = meurt en un coup)")]
    public int hitPoints = 1;

    [Tooltip("Vitesse de déplacement de base")]
    public float baseMoveSpeed = 3f;

    [Tooltip("Dégâts infligés au joueur si l'ennemi explose")]
    public int damage = 10;

    [Header("=== Input requis ===")]
    public EnemyInputType requiredInput = EnemyInputType.Any;

    [Header("=== Durée de vie ===")]
    [Tooltip("Durée de vie minimum en beats (après le spawn). L'ennemi est détruit après ce délai.")]
    public float lifetimeMinBeats = 8f;

    [Tooltip("Durée de vie maximum en beats. Si différent de min, un random entre min et max est choisi.")]
    public float lifetimeMaxBeats = 8f;

    [Header("=== Clignotement avant vulnérabilité ===")]
    [Tooltip("Nombre de beats de clignotement avant de devenir vulnérable")]
    public int blinkBeatsBeforeVulnerable = 6;

    [Header("=== Visuel ===")]
    [Tooltip("Couleur du warning avant le spawn")]
    public Color warningColor = Color.red;

    [Header("=== Spam (si requiredInput == Spam) ===")]
    public int spamClicksRequired = 5;
    public float spamWindowDuration = 1f;
}
