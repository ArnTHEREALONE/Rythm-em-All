using UnityEngine;

/// <summary>
/// Système de combat du joueur. 2 inputs de frappe (gauche/droite).
/// Cible automatiquement l'ennemi vulnérable le plus proche et évalue le timing.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Portée de frappe maximum")]
    public float hitRange = 3f;

    [Tooltip("Délai maximum entre les 2 inputs pour un 'Both' hit (secondes)")]
    public float bothInputWindow = 0.1f;

    [Header("=== SFX ===")]
    public AudioClip sfxPerfect;
    public AudioClip sfxGood;
    public AudioClip sfxMiss;
    public AudioClip sfxTooSoon;
    public AudioClip sfxTooLate;

    [Header("=== État (debug) ===")]
    [SerializeField] private float lastLeftAttackTime = -999f;
    [SerializeField] private float lastRightAttackTime = -999f;

    private PlayerHealth playerHealth;

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    /// <summary>
    /// Appelé quand le joueur appuie sur l'input de frappe gauche.
    /// </summary>
    public void AttackLeft()
    {
        lastLeftAttackTime = Time.time;

        // Vérifie si l'input droit a été pressé récemment → Both
        if (Time.time - lastRightAttackTime <= bothInputWindow)
        {
            PerformAttack(EnemyInputType.Both);
            return;
        }

        PerformAttack(EnemyInputType.LeftOnly);
    }

    /// <summary>
    /// Appelé quand le joueur appuie sur l'input de frappe droite.
    /// </summary>
    public void AttackRight()
    {
        lastRightAttackTime = Time.time;

        // Vérifie si l'input gauche a été pressé récemment → Both
        if (Time.time - lastLeftAttackTime <= bothInputWindow)
        {
            PerformAttack(EnemyInputType.Both);
            return;
        }

        PerformAttack(EnemyInputType.RightOnly);
    }

    /// <summary>
    /// Effectue une attaque avec le type d'input donné.
    /// Cible l'ennemi vulnérable le plus proche.
    /// </summary>
    private void PerformAttack(EnemyInputType inputType)
    {
        if (EnemySpawnManager.Instance == null) return;

        // Trouver l'ennemi le plus proche
        EnemyBase target = EnemySpawnManager.Instance.GetClosestVulnerableEnemy(
            transform.position, inputType);

        if (target == null)
        {
            // Aucun ennemi à portée — hit dans le vide
            // On pourrait déclencher un miss ou juste ne rien faire
            return;
        }

        // Vérifier la portée
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > hitRange)
        {
            return; // Trop loin
        }

        // Tenter de frapper l'ennemi
        TimingResult result = target.TryHit(inputType);

        // Conséquences selon le timing
        HandleTimingResult(result, target);
    }

    /// <summary>
    /// Gère les conséquences d'une frappe selon le résultat du timing.
    /// </summary>
    private void HandleTimingResult(TimingResult result, EnemyBase enemy)
    {
        switch (result)
        {
            case TimingResult.Perfect:
                // Score + speed up (géré par ScoreManager via EnemyBase.OnEnemyKilled)
                PlaySFX(sfxPerfect);
                // Soin on kill
                if (playerHealth != null && enemy.CurrentHP <= 0)
                    playerHealth.HealOnKill();
                break;

            case TimingResult.Good:
                PlaySFX(sfxGood);
                if (playerHealth != null && enemy.CurrentHP <= 0)
                    playerHealth.HealOnKill();
                break;

            case TimingResult.TooSoon:
                // Dégâts réduits au joueur, pas de kill
                PlaySFX(sfxTooSoon);
                if (playerHealth != null)
                {
                    PlayerConfig config = GetComponent<PlayerController>()?.config;
                    int dmg = config != null ? config.damageOnTooSoon : 5;
                    playerHealth.TakeDamage(dmg);
                }
                // Register miss pour le score/speed
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.RegisterMiss();
                // Feedback
                if (TimingFeedbackUI.Instance != null)
                    TimingFeedbackUI.Instance.ShowFeedback(result, enemy.transform.position);
                break;

            case TimingResult.TooLate:
                // Demi-dégât à l'ennemi (déjà géré dans EnemyBase.TryHit)
                PlaySFX(sfxTooLate);
                break;

            case TimingResult.Miss:
                PlaySFX(sfxMiss);
                break;
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Affiche la portée de frappe dans l'éditeur
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRange);
    }
}
