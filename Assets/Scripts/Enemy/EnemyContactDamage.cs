using UnityEngine;

/// <summary>
/// Gère les dégâts de contact entre un ennemi et le joueur.
/// Quand l'ennemi touche le joueur : l'ennemi est détruit et le joueur prend des dégâts.
/// Ajouté dynamiquement par EnemySpawner si EnemyData.damageOnContact == true.
/// Nécessite un Collider (trigger) sur l'ennemi et un Collider sur le joueur.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnemyContactDamage : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Dégâts infligés au joueur au contact.")]
    [SerializeField] private int damage = 10;

    private EnemyBase enemyBase;
    private bool hasDealtDamage;

    private void Awake()
    {
        enemyBase = GetComponent<EnemyBase>();
    }

    /// <summary>
    /// Initialise les dégâts de contact. Appelé par EnemySpawner.
    /// </summary>
    public void Initialize(int contactDamage)
    {
        damage = contactDamage;
        hasDealtDamage = false;

        // S'assurer qu'il y a un trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            // Ne pas modifier un collider existant non-trigger,
            // le level designer doit s'assurer qu'il y a un trigger
            Debug.LogWarning($"EnemyContactDamage: Le Collider sur {gameObject.name} n'est pas un trigger. " +
                "Le contact damage nécessite un Collider en mode 'Is Trigger'.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasDealtDamage) return;

        // Vérifier que c'est le joueur
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null) return;

        // Vérifier que l'ennemi est encore vivant
        if (enemyBase != null &&
            (enemyBase.CurrentState == EnemyBase.EnemyState.Dead ||
             enemyBase.CurrentState == EnemyBase.EnemyState.Exploding))
            return;

        hasDealtDamage = true;

        // Infliger les dégâts au joueur
        playerHealth.TakeDamage(damage);

        // Détruire l'ennemi (comme une explosion mais causée par le contact)
        if (enemyBase != null)
        {
            // On force l'état Dead pour éviter tout double traitement
            // et on détruit l'objet
            Destroy(gameObject, 0.1f);
        }
        else
        {
            Destroy(gameObject, 0.1f);
        }
    }
}
