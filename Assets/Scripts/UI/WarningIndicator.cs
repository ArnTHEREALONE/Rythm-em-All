using UnityEngine;
using System.Collections;

/// <summary>
/// Indicateur visuel de warning sur les bords de l'arène.
/// Clignote quand des ennemis vont bientôt spawn depuis ce côté.
/// </summary>
public class WarningIndicator : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("SpriteRenderer ou Renderer pour l'indicateur")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Fréquence de clignotement (clignote/sec)")]
    public float blinkRate = 3f;

    [Tooltip("Alpha minimum pendant le clignotement")]
    public float minAlpha = 0.2f;

    [Tooltip("Alpha maximum pendant le clignotement")]
    public float maxAlpha = 1f;

    private bool isShowing;
    private Coroutine blinkCoroutine;
    private Color currentColor;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Caché par défaut
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }
    }

    /// <summary>
    /// Affiche le warning avec une couleur et une durée.
    /// </summary>
    public void Show(Color color, float duration)
    {
        currentColor = color;
        isShowing = true;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(BlinkCoroutine(duration));
    }

    /// <summary>
    /// Cache le warning immédiatement.
    /// </summary>
    public void Hide()
    {
        isShowing = false;

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }
    }

    private IEnumerator BlinkCoroutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && isShowing)
        {
            elapsed += Time.deltaTime;

            // Effet sinusoïdal pour le clignotement
            float t = Mathf.PingPong(elapsed * blinkRate, 1f);
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            // Intensifie le clignotement vers la fin
            float urgency = elapsed / duration;
            float finalBlinkRate = blinkRate + urgency * 5f;
            t = Mathf.PingPong(elapsed * finalBlinkRate, 1f);
            alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            if (spriteRenderer != null)
            {
                Color c = currentColor;
                c.a = alpha;
                spriteRenderer.color = c;
            }

            yield return null;
        }

        Hide();
    }
}
