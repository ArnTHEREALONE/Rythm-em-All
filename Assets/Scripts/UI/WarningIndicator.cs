using UnityEngine;
using System.Collections;

public class WarningIndicator : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    public float blinkRate = 3f;

    public float minAlpha = 0.2f;

    public float maxAlpha = 1f;

    private bool isShowing;
    private Coroutine blinkCoroutine;
    private Color currentColor;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }
    }

    public void Show(Color color, float duration)
    {
        currentColor = color;
        isShowing = true;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(BlinkCoroutine(duration));
    }

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

            float t = Mathf.PingPong(elapsed * blinkRate, 1f);
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
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
