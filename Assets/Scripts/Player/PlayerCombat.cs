using UnityEngine;

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

    public void AttackLeft()
    {
        lastLeftAttackTime = Time.time;
        if (Time.time - lastRightAttackTime <= bothInputWindow)
        {
            PerformAttack(EnemyInputType.Both);
            return;
        }

        PerformAttack(EnemyInputType.LeftOnly);
    }

    public void AttackRight()
    {
        lastRightAttackTime = Time.time;
        if (Time.time - lastLeftAttackTime <= bothInputWindow)
        {
            PerformAttack(EnemyInputType.Both);
            return;
        }

        PerformAttack(EnemyInputType.RightOnly);
    }
    private void PerformAttack(EnemyInputType inputType)
    {
        if (EnemySpawnManager.Instance == null) return;

        EnemyBase target = EnemySpawnManager.Instance.GetClosestVulnerableEnemy(
            transform.position, inputType);

        if (target == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > hitRange)
        {
            return;
        }

        TimingResult result = target.TryHit(inputType);
        HandleTimingResult(result, target);
    }

    private void HandleTimingResult(TimingResult result, EnemyBase enemy)
    {
        switch (result)
        {
            case TimingResult.Perfect:
                PlaySFX(sfxPerfect);
                if (playerHealth != null && enemy.CurrentHP <= 0)
                    playerHealth.HealOnKill();
                break;

            case TimingResult.Good:
                PlaySFX(sfxGood);
                if (playerHealth != null && enemy.CurrentHP <= 0)
                    playerHealth.HealOnKill();
                break;

            case TimingResult.TooSoon:
                PlaySFX(sfxTooSoon);
                if (playerHealth != null)
                {
                    PlayerConfig config = GetComponent<PlayerController>()?.config;
                    int dmg = config != null ? config.damageOnTooSoon : 5;
                    playerHealth.TakeDamage(dmg);
                }
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.RegisterMiss();
                if (TimingFeedbackUI.Instance != null)
                    TimingFeedbackUI.Instance.ShowFeedback(result, enemy.transform.position);
                break;

            case TimingResult.TooLate:
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRange);
    }
}
