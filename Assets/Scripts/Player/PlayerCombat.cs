using UnityEngine;
using FMODUnity;
using System.Collections;









public class PlayerCombat : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Portée de frappe maximum")]
    public float hitRange = 3f;

    [Tooltip("Délai maximum entre les 2 inputs pour un 'Both' hit (secondes)")]
    public float bothInputWindow = 0.1f;

    [Header("=== Flash Visuel ===")]
    [Tooltip("Couleur du flash quand le joueur attaque")]
    public Color attackFlashColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Durée du flash en secondes")]
    public float attackFlashDuration = 0.08f;

    [Header("=== Range Visuel (Scene) ===")]
    [Tooltip("Afficher le cercle de range dans le jeu (via LineRenderer)")]
    public bool showRangeInGame = false;

    [Tooltip("Couleur du cercle de range")]
    public Color rangeCircleColor = new Color(1f, 0f, 0f, 0.3f);

    [Header("=== SFX (FMOD Events) ===")]
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
    private Renderer playerRenderer;
    private Color playerOriginalColor;
    private Coroutine flashCoroutine;
    private LineRenderer rangeLineRenderer;

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerRenderer = GetComponentInChildren<Renderer>();

        
        var controller = GetComponent<PlayerController>();
        if (controller != null)
            playerConfig = controller.config;

        if (playerRenderer != null)
            playerOriginalColor = playerRenderer.material.color;

        
        if (showRangeInGame)
            CreateRangeCircle();
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
        
        FlashPlayer();

        if (EnemySpawnManager.Instance == null) return;

        
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
                PlaySFX(sfxPerfect);
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.RegisterHit(result);
                if (playerHealth != null && enemy.CurrentHP <= 0)
                    playerHealth.HealOnKill();
                if (TimingFeedbackUI.Instance != null)
                    TimingFeedbackUI.Instance.ShowFeedback(result, enemy.transform.position);
                break;

            case TimingResult.Good:
                PlaySFX(sfxGood);
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.RegisterHit(result);
                if (playerHealth != null && enemy.CurrentHP <= 0)
                    playerHealth.HealOnKill();
                if (TimingFeedbackUI.Instance != null)
                    TimingFeedbackUI.Instance.ShowFeedback(result, enemy.transform.position);
                break;

            case TimingResult.TooSoon:
                
                PlaySFX(sfxTooSoon);
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
                
                PlaySFX(sfxTooLate);
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
                PlaySFX(sfxMiss);
                break;
        }
    }

    
    
    
    private void FlashPlayer()
    {
        if (playerRenderer == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        if (playerRenderer != null)
            playerRenderer.material.color = attackFlashColor;

        yield return new WaitForSeconds(attackFlashDuration);

        if (playerRenderer != null)
            playerRenderer.material.color = playerOriginalColor;

        flashCoroutine = null;
    }

    private void PlaySFX(EventReference sfxEvent)
    {
        if (!sfxEvent.IsNull && FMODAudioManager.Instance != null)
        {
            FMODAudioManager.Instance.PlaySFX(sfxEvent);
        }
    }

    
    
    

    private void CreateRangeCircle()
    {
        GameObject rangeGO = new GameObject("AttackRangeCircle");
        rangeGO.transform.SetParent(transform);
        rangeGO.transform.localPosition = Vector3.zero;

        rangeLineRenderer = rangeGO.AddComponent<LineRenderer>();
        rangeLineRenderer.useWorldSpace = false;
        rangeLineRenderer.loop = true;
        rangeLineRenderer.startWidth = 0.05f;
        rangeLineRenderer.endWidth = 0.05f;
        rangeLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        rangeLineRenderer.startColor = rangeCircleColor;
        rangeLineRenderer.endColor = rangeCircleColor;

        int segments = 64;
        rangeLineRenderer.positionCount = segments;
        UpdateRangeCircle();
    }

    private void UpdateRangeCircle()
    {
        if (rangeLineRenderer == null) return;
        int segments = rangeLineRenderer.positionCount;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * hitRange;
            float z = Mathf.Sin(angle) * hitRange;
            rangeLineRenderer.SetPosition(i, new Vector3(x, 0.05f, z));
        }
    }

    private void Update()
    {
        if (rangeLineRenderer != null)
            UpdateRangeCircle();
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
