using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;








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

    
    private float totalBeats = 128f;

    private void Start()
    {
        if (miniSlider != null)
        {
            miniSlider.onValueChanged.AddListener(OnMiniSliderChanged);
        }
    }

    
    
    
    
    public void Refresh()
    {
        if (BeatMapEditor.Instance == null) return;

        var map = BeatMapEditor.Instance.CurrentMap;
        if (map == null) return;

        float currentBeat = BeatMapEditor.Instance.CurrentBeat;

        
        UpdateTotalBeats(map);

        
        UpdateBandSize();

        
        UpdateBandPosition(currentBeat);

        
        if (currentBeatText != null)
            currentBeatText.text = $"Beat: {currentBeat:F1}";

        if (currentTimeText != null)
        {
            float seconds = map.BeatToSeconds(currentBeat);
            int min = (int)(seconds / 60);
            int sec = (int)(seconds % 60);
            currentTimeText.text = $"{min:D2}:{sec:D2}";
        }

        
        if (miniSlider != null && !isDragging && totalBeats > 0)
        {
            miniSlider.SetValueWithoutNotify(currentBeat / totalBeats);
        }
    }

    
    
    
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

    
    
    
    private void UpdateBandSize()
    {
        if (timelineBand == null) return;

        float totalWidth = totalBeats * pixelsPerBeat;
        Vector2 size = timelineBand.sizeDelta;
        size.x = Mathf.Max(totalWidth, 100f);
        timelineBand.sizeDelta = size;
    }

    
    
    
    private void UpdateBandPosition(float currentBeat)
    {
        if (timelineBand == null || viewport == null) return;

        float viewportWidth = viewport.rect.width;
        float beatX = currentBeat * pixelsPerBeat;

        
        float offsetX = -(beatX - viewportWidth / 2f);

        
        float totalWidth = timelineBand.sizeDelta.x;
        float maxOffset = 0f;
        float minOffset = -(totalWidth - viewportWidth);

        if (totalWidth > viewportWidth)
            offsetX = Mathf.Clamp(offsetX, minOffset, maxOffset);

        Vector2 pos = timelineBand.anchoredPosition;
        pos.x = offsetX;
        timelineBand.anchoredPosition = pos;
    }

    

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
        float deltaBeats = -deltaPixels / pixelsPerBeat; 

        float targetBeat = Mathf.Max(0f, dragStartBeat + deltaBeats);
        float currentBeat = BeatMapEditor.Instance.CurrentBeat;
        BeatMapEditor.Instance.MoveBeat(targetBeat - currentBeat);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    

    private void OnMiniSliderChanged(float value)
    {
        if (isDragging) return;
        if (BeatMapEditor.Instance == null) return;

        float targetBeat = value * totalBeats;
        BeatMapEditor.Instance.GoToBeat(targetBeat);
    }

    
    
    
    public float GetTotalBeats()
    {
        return totalBeats;
    }
}
