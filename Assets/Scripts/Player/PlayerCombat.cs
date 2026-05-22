using UnityEngine;
using FMODUnity;
using System;


/// <summary>
/// Gère le combat du joueur : attaques gauche/droite, timing, SFX.
/// Le flash visuel et le cercle de range sont maintenant gérés par PlayerVFX.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Portée de frappe maximum")]
    public float hitRange = 3f;

    [Tooltip("Délai maximum entre les 2 inputs pour un 'Both' hit (secondes)")]
    public float bothInputWindow = 0.1f;

    [Header("=== SFX (FMOD Events) ===")]
    public EventReference attackSFX;
    public EventReference sfxPerfect;
    public EventReference sfxGood;
    public EventReference sfxMiss;
    public EventReference sfxTooSoon;
    public EventReference sfxTooLate;

    [Header("=== État (debug) ===")]
    [SerializeField] private float lastLeftAttackTime = -999f;
    [SerializeField] private float lastRightAttackTime = -999f;

    private PlayerHealth playerHealth;
    private PlayerConfig playerConfig;

    public event Action OnAttackPerformed;

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();

        var controller = GetComponent<PlayerController>();
        if (controller != null)
            playerConfig = controller.config;
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
        // Appel direct sans indirection singleton pour latence minimale
        if (!attackSFX.IsNull)
            RuntimeManager.PlayOneShot(attackSFX);

        OnAttackPerformed?.Invoke();

        if (EnemySpawnManager.Instance == null) return;

        // Touche UN SEUL ennemi : le plus proche dans la range
        EnemyBase target = EnemySpawnManager.Instance.GetClosestEnemy(
            transform.position, inputType, hitRange);

        if (target == null) return;

        TimingResult result = target.TryHit(inputType);
        HandleTimingResult(result, target);
    }

    private void HandleTimingResult(TimingResult result, EnemyBase enemy)
    {
        switch (result)
        {
            case TimingResult.Perfect:
                if (!sfxPerfect.IsNull) RuntimeManager.PlayOneShot(sfxPerfect);
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.RegisterHit(result);
                if (playerHealth != null && enemy.CurrentHP <= 0)
                    playerHealth.HealOnKill();
                if (TimingFeedbackUI.Instance != null)
                    TimingFeedbackUI.Instance.ShowFeedback(result, enemy.transform.position);
                break;

            case TimingResult.Good:
                if (!sfxGood.IsNull) RuntimeManager.PlayOneShot(sfxGood);
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.RegisterHit(result);
                if (playerHealth != null && enemy.CurrentHP <= 0)
                    playerHealth.HealOnKill();
                if (TimingFeedbackUI.Instance != null)
                    TimingFeedbackUI.Instance.ShowFeedback(result, enemy.transform.position);
                break;

            case TimingResult.TooSoon:
                if (!sfxTooSoon.IsNull) RuntimeManager.PlayOneShot(sfxTooSoon);
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.RegisterMiss();
                if (playerHealth != null)
                {
                    int dmg = playerConfig != null ? playerConfig.damageOnTooSoon : 5;
                    playerHealth.TakeDamage(dmg);
                }
                if (TimingFeedbackUI.Instance != null)
                    TimingFeedbackUI.Instance.ShowFeedback(result, enemy.transform.position);
                break;

            case TimingResult.TooLate:
                if (!sfxTooLate.IsNull) RuntimeManager.PlayOneShot(sfxTooLate);
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.RegisterHit(result);
                if (playerHealth != null)
                {
                    int dmg = playerConfig != null ? playerConfig.damageOnTooLate : 3;
                    playerHealth.TakeDamage(dmg);
                }
                if (TimingFeedbackUI.Instance != null)
                    TimingFeedbackUI.Instance.ShowFeedback(result, enemy.transform.position);
                break;

            case TimingResult.Miss:
                if (!sfxMiss.IsNull) RuntimeManager.PlayOneShot(sfxMiss);
                break;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        DrawGizmoCircle(transform.position, hitRange, 64);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        DrawGizmoCircle(transform.position, hitRange, 64);
    }

    private void DrawGizmoCircle(Vector3 center, float radius, int segments)
    {
        Vector3 prevPoint = center + new Vector3(radius, 0.05f, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}
