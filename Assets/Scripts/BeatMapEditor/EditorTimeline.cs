using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Timeline de l'éditeur de beatmap.
/// Affiche une bande horizontale colorée proportionnelle à la durée de la musique,
/// avec des beat markers verticaux et 12 lignes de lanes horizontales.
/// La bande est grabbable pour naviguer horizontalement.
/// Un mini-slider en bas indique la position globale dans la timeline.
/// </summary>
public class EditorTimeline : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("=== Références ===")]
    [Tooltip("Le RectTransform de la bande de timeline (la zone scrollable)")]
    public RectTransform timelineBand;

    [Tooltip("Le RectTransform du viewport (zone visible)")]
    public RectTransform viewport;

    [Tooltip("Mini-slider de navigation en bas de l'écran")]
    public Slider miniSlider;

    [Tooltip("Texte affichant le beat actuel")]
    public TextMeshProUGUI currentBeatText;

    [Tooltip("Texte affichant le temps actuel (mm:ss)")]
    public TextMeshProUGUI currentTimeText;

    [Header("=== Configuration ===")]
    [Tooltip("Nombre de pixels par beat — détermine la longueur totale de la timeline")]
    public float pixelsPerBeat = 40f;

    [Tooltip("Nombre de lanes")]
    public int laneCount = 12;

    [Header("=== Couleurs ===")]
    public Color bandColor = new Color(0.12f, 0.14f, 0.22f, 0.9f);
    public Color beatMarkerColor = new Color(1f, 1f, 1f, 0.2f);
    public Color strongBeatMarkerColor = new Color(1f, 1f, 1f, 0.45f);
    public Color laneLineColor = new Color(1f, 1f, 1f, 0.06f);
    public Color playheadColor = new Color(0.91f, 0.27f, 0.38f, 1f);

    private bool isDragging;
    private float dragStartX;
    private float dragStartBeat;

    // Nombre total de beats dans la musique (calculé dynamiquement via FMOD)
    private float totalBeats = 128f;

    private void Start()
    {
        if (miniSlider != null)
        {
            miniSlider.onValueChanged.AddListener(OnMiniSliderChanged);
        }
    }

    /// <summary>
    /// Rafraîchit la timeline : met à jour la taille de la bande,
    /// les textes, et la position du mini-slider.
    /// </summary>
    public void Refresh()
    {
        if (BeatMapEditor.Instance == null) return;

        var map = BeatMapEditor.Instance.CurrentMap;
        if (map == null) return;

        float currentBeat = BeatMapEditor.Instance.CurrentBeat;

        // Calculer le nombre total de beats depuis la durée FMOD
        UpdateTotalBeats(map);

        // Mettre à jour la taille de la bande de timeline
        UpdateBandSize();

        // Mettre à jour la position (scroll) de la bande
        UpdateBandPosition(currentBeat);

        // Textes
        if (currentBeatText != null)
            currentBeatText.text = $"Beat: {currentBeat:F1}";

        if (currentTimeText != null)
        {
            float seconds = map.BeatToSeconds(currentBeat);
            int min = (int)(seconds / 60);
            int sec = (int)(seconds % 60);
            currentTimeText.text = $"{min:D2}:{sec:D2}";
        }

        // Mini-slider
        if (miniSlider != null && !isDragging && totalBeats > 0)
        {
            miniSlider.SetValueWithoutNotify(currentBeat / totalBeats);
        }
    }

    /// <summary>
    /// Met à jour le nombre total de beats depuis la durée FMOD.
    /// </summary>
    private void UpdateTotalBeats(BeatMapData map)
    {
        if (FMODAudioManager.Instance != null && FMODAudioManager.Instance.GetMusicLengthSeconds() > 0)
        {
            if (map.bpm > 0)
            {
                totalBeats = map.SecondsToBeat(FMODAudioManager.Instance.GetMusicLengthSeconds());
            }
        }
        else
        {
            // Fallback : si pas de musique chargée, utiliser les notes existantes
            if (map.notes != null && map.notes.Count > 0)
            {
                totalBeats = map.notes[map.notes.Count - 1].beatTime + 16f;
            }
            else
            {
                totalBeats = 128f;
            }
        }
    }

    /// <summary>
    /// Ajuste la largeur de la bande de timeline selon pixelsPerBeat × totalBeats.
    /// </summary>
    private void UpdateBandSize()
    {
        if (timelineBand == null) return;

        float totalWidth = totalBeats * pixelsPerBeat;
        Vector2 size = timelineBand.sizeDelta;
        size.x = Mathf.Max(totalWidth, 100f);
        timelineBand.sizeDelta = size;
    }

    /// <summary>
    /// Positionne la bande pour que le beat actuel soit centré dans le viewport.
    /// </summary>
    private void UpdateBandPosition(float currentBeat)
    {
        if (timelineBand == null || viewport == null) return;

        float viewportWidth = viewport.rect.width;
        float beatX = currentBeat * pixelsPerBeat;

        // Centrer le beat actuel dans le viewport
        float offsetX = -(beatX - viewportWidth / 2f);

        // Clamper pour ne pas montrer du vide avant le début ou après la fin
        float totalWidth = timelineBand.sizeDelta.x;
        float maxOffset = 0f;
        float minOffset = -(totalWidth - viewportWidth);

        if (totalWidth > viewportWidth)
            offsetX = Mathf.Clamp(offsetX, minOffset, maxOffset);

        Vector2 pos = timelineBand.anchoredPosition;
        pos.x = offsetX;
        timelineBand.anchoredPosition = pos;
    }

    // === Drag pour naviguer dans la timeline ===

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (BeatMapEditor.Instance == null || BeatMapEditor.Instance.IsPlaying) return;

        isDragging = true;
        dragStartX = eventData.position.x;
        dragStartBeat = BeatMapEditor.Instance.CurrentBeat;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || BeatMapEditor.Instance == null) return;

        float deltaPixels = eventData.position.x - dragStartX;
        float deltaBeats = -deltaPixels / pixelsPerBeat; // Négatif car on drag dans le sens opposé

        float targetBeat = Mathf.Max(0f, dragStartBeat + deltaBeats);
        float currentBeat = BeatMapEditor.Instance.CurrentBeat;
        BeatMapEditor.Instance.MoveBeat(targetBeat - currentBeat);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    // === Mini-slider ===

    private void OnMiniSliderChanged(float value)
    {
        if (isDragging) return;
        if (BeatMapEditor.Instance == null) return;

        float targetBeat = value * totalBeats;
        BeatMapEditor.Instance.GoToBeat(targetBeat);
    }

    /// <summary>
    /// Retourne le nombre total de beats (pour d'autres scripts).
    /// </summary>
    public float GetTotalBeats()
    {
        return totalBeats;
    }
}
