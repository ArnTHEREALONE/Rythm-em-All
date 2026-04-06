using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Affiche le feedback visuel du timing (PERFECT, GOOD, TOO SOON, etc.)
/// au-dessus de la position de l'ennemi frappé. Fade out automatique.
/// </summary>
public class TimingFeedbackUI : MonoBehaviour
{
    public static TimingFeedbackUI Instance { get; private set; }

    [Header("=== Références ===")]
    [Tooltip("Prefab du feedback (World Space Canvas avec TextMeshPro)")]
    public GameObject feedbackPrefab;

    [Header("=== Configuration ===")]
    [Tooltip("Durée d'affichage du feedback en secondes")]
    public float displayDuration = 0.6f;

    [Tooltip("Distance de montée du texte pendant le fade")]
    public float floatUpDistance = 1.5f;

    [Header("=== Screen Space Feedback ===")]
    [Tooltip("TextMeshPro central pour feedback (optionnel, en plus du world space)")]
    public TextMeshProUGUI screenFeedbackText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Affiche un feedback en world space à une position donnée.
    /// </summary>
    public void ShowFeedback(TimingResult result, Vector3 worldPosition)
    {
        // Feedback world space (prefab)
        if (feedbackPrefab != null)
        {
            GameObject fb = Instantiate(feedbackPrefab, worldPosition + Vector3.up * 0.5f, Quaternion.identity);
            StartCoroutine(AnimateFeedback(fb, result));
        }

        // Feedback screen space (texte central)
        if (screenFeedbackText != null)
        {
            StartCoroutine(ShowScreenFeedback(result));
        }
    }

    /// <summary>
    /// Affiche un feedback au centre de l'écran uniquement.
    /// </summary>
    public void ShowScreenFeedbackOnly(TimingResult result)
    {
        if (screenFeedbackText != null)
        {
            StartCoroutine(ShowScreenFeedback(result));
        }
    }

    private IEnumerator AnimateFeedback(GameObject fb, TimingResult result)
    {
        TextMeshProUGUI text = fb.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null)
        {
            // Essaye avec TextMeshPro normal (world space)
            TextMeshPro text3D = fb.GetComponentInChildren<TextMeshPro>();
            if (text3D != null)
            {
                text3D.text = TimingJudge.GetFeedbackText(result);
                text3D.color = TimingJudge.GetFeedbackColor(result);

                // Scale selon le résultat
                float scale = result == TimingResult.Perfect ? 1.5f : 1f;
                fb.transform.localScale = Vector3.one * scale;
            }
        }
        else
        {
            text.text = TimingJudge.GetFeedbackText(result);
            text.color = TimingJudge.GetFeedbackColor(result);
        }

        Vector3 startPos = fb.transform.position;
        float elapsed = 0f;

        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / displayDuration;

            // Monte doucement
            fb.transform.position = startPos + Vector3.up * (floatUpDistance * t);

            // Fade out
            float alpha = 1f - Mathf.Pow(t, 2f);
            SetAlpha(fb, alpha);

            yield return null;
        }

        Destroy(fb);
    }

    private IEnumerator ShowScreenFeedback(TimingResult result)
    {
        screenFeedbackText.text = TimingJudge.GetFeedbackText(result);
        screenFeedbackText.color = TimingJudge.GetFeedbackColor(result);

        // Scale punch pour Perfect
        float scale = result == TimingResult.Perfect ? 1.3f : 1f;
        screenFeedbackText.transform.localScale = Vector3.one * scale;

        float elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / displayDuration;

            Color c = screenFeedbackText.color;
            c.a = 1f - Mathf.Pow(t, 2f);
            screenFeedbackText.color = c;

            // Retour au scale normal
            float s = Mathf.Lerp(scale, 1f, t);
            screenFeedbackText.transform.localScale = Vector3.one * s;

            yield return null;
        }

        screenFeedbackText.text = "";
    }

    private void SetAlpha(GameObject go, float alpha)
    {
        var texts = go.GetComponentsInChildren<TextMeshPro>();
        foreach (var t in texts)
        {
            Color c = t.color;
            c.a = alpha;
            t.color = c;
        }

        var textsUI = go.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var t in textsUI)
        {
            Color c = t.color;
            c.a = alpha;
            t.color = c;
        }
    }
}
