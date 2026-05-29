using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Spawns burst particles from the player that fly toward the HP bar on heal-on-kill.
/// Uses a manually-created Unity Particle System prefab (burst mode).
/// After the burst, the particles are attracted toward the HP bar's world position.
/// Attach to the Player GameObject.
/// </summary>
public class HealParticlesFly : MonoBehaviour
{
    [Header("=== Références ===")]
    [Tooltip("Le PlayerHealth pour détecter les heals on kill.")]
    public PlayerHealth playerHealth;

    [Tooltip("Le Slider de HP (UI) vers lequel les particules volent.")]
    public Slider hpBarSlider;

    [Header("=== Prefab de particules ===")]
    [Tooltip("Prefab ParticleSystem (burst, world space). Créer manuellement dans Unity.\n" +
             "Les particules doivent être configurées en Simulation Space = World.")]
    public ParticleSystem healParticlesPrefab;

    [Header("=== Paramètres du vol ===")]
    [Tooltip("Délai avant que les particules commencent à voler vers la barre de PV (en secondes).\n" +
             "Permet au burst de se déployer avant d'être attiré.")]
    public float flyDelay = 0.3f;

    [Tooltip("Vitesse de déplacement des particules vers la barre de PV.")]
    public float flySpeed = 15f;

    [Tooltip("Force d'attraction (lerp factor par seconde). Plus c'est haut, plus c'est direct.")]
    public float attractionStrength = 5f;

    [Tooltip("Distance à laquelle les particules sont considérées arrivées et sont détruites.")]
    public float arrivalDistance = 0.5f;

    [Tooltip("Durée de vie max de l'effet (auto-destroy fallback).")]
    public float maxLifetime = 2f;

    private Camera mainCamera;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        mainCamera = Camera.main;

        if (playerHealth != null)
            playerHealth.OnHealActive += HandleHealOnKill;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealActive -= HandleHealOnKill;
    }

    private void HandleHealOnKill()
    {
        if (healParticlesPrefab == null || hpBarSlider == null) return;

        // Spawn les particules à la position du joueur
        ParticleSystem ps = Instantiate(healParticlesPrefab, transform.position, Quaternion.identity);
        ps.Play();

        // Lancer la coroutine qui guide les particules vers la barre de PV
        StartCoroutine(FlyParticlesToHPBar(ps));
    }

    private IEnumerator FlyParticlesToHPBar(ParticleSystem ps)
    {
        // Phase 1 : laisser le burst se déployer
        yield return new WaitForSeconds(flyDelay);

        if (ps == null) yield break;

        // Calculer la position cible (barre de PV en world space)
        Vector3 targetWorldPos = GetHPBarWorldPosition();

        float elapsed = 0f;
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];

        while (elapsed < maxLifetime && ps != null)
        {
            elapsed += Time.deltaTime;

            // Mettre à jour la position cible (la barre peut bouger si la caméra bouge)
            targetWorldPos = GetHPBarWorldPosition();

            int count = ps.GetParticles(particles);

            if (count == 0)
            {
                // Plus de particules, c'est fini
                break;
            }

            bool allArrived = true;

            for (int i = 0; i < count; i++)
            {
                Vector3 dir = targetWorldPos - particles[i].position;
                float dist = dir.magnitude;

                if (dist > arrivalDistance)
                {
                    allArrived = false;
                    // Attirer la particule vers la cible
                    Vector3 attraction = dir.normalized * flySpeed * Time.deltaTime;
                    particles[i].velocity = Vector3.Lerp(
                        particles[i].velocity,
                        dir.normalized * flySpeed,
                        Time.deltaTime * attractionStrength
                    );
                }
                else
                {
                    // Particule arrivée : la tuer
                    particles[i].remainingLifetime = 0f;
                }
            }

            ps.SetParticles(particles, count);

            if (allArrived) break;

            yield return null;
        }

        // Cleanup
        if (ps != null)
            Destroy(ps.gameObject, 0.5f);
    }

    /// <summary>
    /// Convertit la position UI du slider HP en position monde pour le vol des particules.
    /// </summary>
    private Vector3 GetHPBarWorldPosition()
    {
        if (hpBarSlider == null || mainCamera == null)
            return transform.position + Vector3.up * 3f;

        // Obtenir la position screen du slider
        RectTransform rt = hpBarSlider.GetComponent<RectTransform>();
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, rt.position);

        // Convertir en position monde (à une distance fixe de la caméra)
        // On place la cible à mi-chemin entre la caméra et le sol
        float zDist = mainCamera.transform.position.y * 0.5f;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));

        return worldPos;
    }
}
