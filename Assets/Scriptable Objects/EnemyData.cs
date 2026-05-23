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

    [Tooltip("Dégâts infligés au joueur si l'ennemi explose (auto-destruction)")]
    public int damage = 10;

    [Header("=== Input requis ===")]
    public EnemyInputType requiredInput = EnemyInputType.Any;

    [Header("=== Durée de vie ===")]
    [Tooltip("Durée en beats entre le SPAWN et le moment où l'ennemi devient vulnérable (le timing parfait).\n" +
             "Plus cette valeur est grande, plus l'ennemi avance longtemps avant de pouvoir être frappé.\n" +
             "Remplace l'ancien 'lookAheadBeats' du SpawnManager.")]
    public float beatsBeforeVulnerable = 4f;

    [Tooltip("Durée en beats pendant laquelle l'ennemi reste vulnérable avant de s'auto-détruire.\n" +
             "Si le joueur ne le frappe pas dans ce délai, l'ennemi explose et inflige des dégâts.\n" +
             "Doit être supérieur aux fenêtres de timing (goodWindow × 2 minimum).")]
    public float vulnerableWindowBeats = 2f;

    [Header("=== Clignotement avant vulnérabilité ===")]
    [Tooltip("Nombre de beats de clignotement avant de devenir vulnérable")]
    public int blinkBeatsBeforeVulnerable = 6;

    [Header("=== Visuel ===")]
    [Tooltip("Couleur du warning avant le spawn")]
    public Color warningColor = Color.red;

    [Header("=== Spam (si requiredInput == Spam) ===")]
    public int spamClicksRequired = 5;
    public float spamWindowDuration = 1f;

    [Header("=== SFX (FMOD) ===")]
    [Tooltip("Son joué quand l'ennemi est tué par le joueur")]
    public FMODUnity.EventReference deathByPlayerSFX;

    [Tooltip("Son joué quand l'ennemi s'auto-détruit (inflige des dégâts)")]
    public FMODUnity.EventReference deathNaturalSFX;
}
