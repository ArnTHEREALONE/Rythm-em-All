using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EditorTimeline : MonoBehaviour
{
    [Header("=== Références ===")]
    public Slider timelineSlider;
    public TextMeshProUGUI currentBeatText;
    public TextMeshProUGUI currentTimeText;

    private bool isDragging;

    private void Start()
    {
        if (timelineSlider != null)
        {
            timelineSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    public void Refresh()
    {
        if (BeatMapEditor.Instance == null) return;

        var map = BeatMapEditor.Instance.CurrentMap;
        if (map == null) return;

        float currentBeat = BeatMapEditor.Instance.CurrentBeat;

        if (currentBeatText != null)
            currentBeatText.text = $"Beat: {currentBeat:F1}";

        if (currentTimeText != null)
        {
            float seconds = map.BeatToSeconds(currentBeat);
            int min = (int)(seconds / 60);
            int sec = (int)(seconds % 60);
            currentTimeText.text = $"{min:D2}:{sec:D2}";
        }

        if (timelineSlider != null && !isDragging)
        {
            float maxBeats = GetMaxBeats();
            if (maxBeats > 0)
            {
                timelineSlider.SetValueWithoutNotify(currentBeat / maxBeats);
            }
        }
    }

    private void OnSliderChanged(float value)
    {
        if (BeatMapEditor.Instance == null) return;

        float maxBeats = GetMaxBeats();
        float targetBeat = value * maxBeats;

        float delta = targetBeat - BeatMapEditor.Instance.CurrentBeat;
        BeatMapEditor.Instance.MoveBeat(delta);
    }

    private float GetMaxBeats()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.MusicLength > 0)
        {
            var map = BeatMapEditor.Instance.CurrentMap;
            if (map != null && map.bpm > 0)
            {
                return map.SecondsToBeat(AudioManager.Instance.MusicLength);
            }
        }

        var currentMap = BeatMapEditor.Instance?.CurrentMap;
        if (currentMap != null && currentMap.notes.Count > 0)
        {
            return currentMap.notes[currentMap.notes.Count - 1].beatTime + 16f;
        }

        return 128f;
    }

    public void OnBeginDrag() => isDragging = true;
    public void OnEndDrag() => isDragging = false;
}
