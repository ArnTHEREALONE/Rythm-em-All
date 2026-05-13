using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;






public class EditorGrid : MonoBehaviour, IPointerClickHandler
{
    [Header("=== Configuration ===")]
    [Tooltip("Nombre de lanes (= nombre de spawners, doit correspondre à BeatMapEditor.laneCount)")]
    public int laneCount = 12;

    [Tooltip("Nombre de beats visibles à l'écran")]
    public float visibleBeats = 16f;

    [Header("=== Références ===")]
    public RectTransform gridArea;
    public GameObject notePrefab;
    public GameObject cursorPrefab;

    [Header("=== Couleurs ===")]
    public Color colorAny = new Color(0.8f, 0.8f, 0.8f);
    public Color colorLeft = new Color(0.3f, 0.5f, 1f);
    public Color colorRight = new Color(1f, 0.3f, 0.3f);
    public Color colorBoth = new Color(0.7f, 0.3f, 1f);
    public Color colorSpam = new Color(1f, 0.8f, 0.2f);
    public Color colorCursor = new Color(1f, 1f, 1f, 0.5f);
    public Color colorBeatLine = new Color(1f, 1f, 1f, 0.15f);
    public Color colorStrongBeatLine = new Color(1f, 1f, 1f, 0.35f);
    public Color colorLaneLine = new Color(1f, 1f, 1f, 0.08f);

    [Header("=== Note Visuel ===")]
    [Tooltip("Diamètre du rond de note en pixels")]
    public float noteDiameter = 28f;

    private List<GameObject> noteVisuals = new List<GameObject>();
    private List<GameObject> gridLines = new List<GameObject>();
    private GameObject cursorVisual;

    
    private BeatNote draggedNote;
    private GameObject draggedVisual;

    private void Start()
    {
        CreateCursor();
    }

    public void Refresh()
    {
        ClearNotes();
        ClearGridLines();

        if (BeatMapEditor.Instance == null) return;

        var map = BeatMapEditor.Instance.CurrentMap;
        if (map == null || map.notes == null) return;

        float currentBeat = BeatMapEditor.Instance.CurrentBeat;
        float startBeat = currentBeat - visibleBeats / 4f;
        float endBeat = currentBeat + visibleBeats * 3f / 4f;

        DrawGridLines(startBeat, endBeat);

        foreach (var note in map.notes)
        {
            if (note.beatTime >= startBeat && note.beatTime <= endBeat)
            {
                if (note.spawnerIndex >= 0 && note.spawnerIndex < laneCount)
                {
                    CreateNoteVisual(note, startBeat, endBeat);
                }
            }
        }

        UpdateCursor(currentBeat, startBeat, endBeat);
    }

    
    
    
    
    private void DrawGridLines(float startBeat, float endBeat)
    {
        if (gridArea == null) return;

        float gridWidth = gridArea.rect.width;
        float gridHeight = gridArea.rect.height;

        
        for (int i = 0; i <= laneCount; i++)
        {
            float x = (float)i / laneCount * gridWidth - gridWidth / 2f;
            CreateLine(new Vector2(x, -gridHeight / 2f), new Vector2(x, gridHeight / 2f), colorLaneLine, 1f);
        }

        
        int startBeatInt = Mathf.FloorToInt(startBeat);
        int endBeatInt = Mathf.CeilToInt(endBeat);

        for (int b = startBeatInt; b <= endBeatInt; b++)
        {
            if (b < 0) continue;

            float beatRange = endBeat - startBeat;
            float normalizedBeat = (b - startBeat) / beatRange;
            float y = (normalizedBeat - 0.5f) * gridHeight;

            bool isStrongBeat = (b % 4 == 0);
            Color lineColor = isStrongBeat ? colorStrongBeatLine : colorBeatLine;
            float thickness = isStrongBeat ? 2f : 1f;

            CreateLine(new Vector2(-gridWidth / 2f, y), new Vector2(gridWidth / 2f, y), lineColor, thickness);
        }
    }

    private void CreateLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        if (gridArea == null) return;

        GameObject lineGO = new GameObject("GridLine");
        lineGO.transform.SetParent(gridArea, false);

        var img = lineGO.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        var rect = lineGO.GetComponent<RectTransform>();

        Vector2 midpoint = (start + end) / 2f;
        Vector2 diff = end - start;
        float length = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        rect.anchoredPosition = midpoint;
        rect.sizeDelta = new Vector2(length, thickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);

        gridLines.Add(lineGO);
    }

    private void CreateNoteVisual(BeatNote note, float startBeat, float endBeat)
    {
        if (gridArea == null) return;

        GameObject noteGO;
        if (notePrefab != null)
        {
            noteGO = Instantiate(notePrefab, gridArea);
        }
        else
        {
            
            noteGO = new GameObject($"Note_{note.beatTime}_{note.spawnerIndex}");
            noteGO.transform.SetParent(gridArea, false);
            var img = noteGO.AddComponent<Image>();
            img.color = GetColorForInputType(note.inputType);

            
            
            var rect = noteGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(noteDiameter, noteDiameter);
        }

        PositionInGrid(noteGO, note.spawnerIndex, note.beatTime, startBeat, endBeat);

        Image image = noteGO.GetComponent<Image>();
        if (image != null)
            image.color = GetColorForInputType(note.inputType);

        
        var dragHandler = noteGO.AddComponent<EditorNoteDragHandler>();
        dragHandler.Initialize(note, this);

        noteVisuals.Add(noteGO);
    }

    
    
    
    public void PositionInGrid(GameObject go, int lane, float beat, float startBeat, float endBeat)
    {
        if (gridArea == null) return;

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) return;

        float gridWidth = gridArea.rect.width;
        float gridHeight = gridArea.rect.height;

        float laneWidth = gridWidth / laneCount;
        float x = (lane + 0.5f) * laneWidth - gridWidth / 2f;

        float beatRange = endBeat - startBeat;
        float normalizedBeat = (beat - startBeat) / beatRange;
        float y = (normalizedBeat - 0.5f) * gridHeight;

        rect.anchoredPosition = new Vector2(x, y);
    }

    
    
    
    
    public (int lane, float beat) ScreenToGrid(Vector2 localPosition)
    {
        if (gridArea == null) return (0, 0);

        float gridWidth = gridArea.rect.width;
        float gridHeight = gridArea.rect.height;

        float currentBeat = BeatMapEditor.Instance?.CurrentBeat ?? 0f;
        float startBeat = currentBeat - visibleBeats / 4f;
        float endBeat = currentBeat + visibleBeats * 3f / 4f;

        
        float laneWidth = gridWidth / laneCount;
        int lane = Mathf.Clamp(Mathf.FloorToInt((localPosition.x + gridWidth / 2f) / laneWidth), 0, laneCount - 1);

        float normalizedBeat = (localPosition.y / gridHeight) + 0.5f;
        float beat = startBeat + normalizedBeat * (endBeat - startBeat);
        float snappedBeat = Mathf.Round(beat);

        return (lane, snappedBeat);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BeatMapEditor.Instance == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(gridArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        var (lane, beat) = ScreenToGrid(localPoint);

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            BeatMapEditor.Instance.DeleteNoteAt(beat, lane);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            BeatMapEditor.Instance.PlaceNoteAt(beat, lane, EnemyInputType.Any);
        }
    }

    private void UpdateCursor(float currentBeat, float startBeat, float endBeat)
    {
        if (cursorVisual == null || gridArea == null) return;

        int selectedLane = BeatMapEditor.Instance?.SelectedLane ?? 0;
        PositionInGrid(cursorVisual, selectedLane, currentBeat, startBeat, endBeat);
    }

    private void CreateCursor()
    {
        if (gridArea == null) return;

        if (cursorPrefab != null)
        {
            cursorVisual = Instantiate(cursorPrefab, gridArea);
        }
        else
        {
            cursorVisual = new GameObject("Cursor");
            cursorVisual.transform.SetParent(gridArea, false);

            var img = cursorVisual.AddComponent<Image>();
            img.color = colorCursor;
            img.raycastTarget = false;

            var rect = cursorVisual.GetComponent<RectTransform>();
            float laneWidth = gridArea.rect.width / Mathf.Max(1, laneCount);
            rect.sizeDelta = new Vector2(laneWidth * 0.9f, gridArea.rect.height / visibleBeats * 0.8f);
        }
    }

    private void ClearNotes()
    {
        foreach (var go in noteVisuals)
        {
            if (go != null) Destroy(go);
        }
        noteVisuals.Clear();
    }

    private void ClearGridLines()
    {
        foreach (var go in gridLines)
        {
            if (go != null) Destroy(go);
        }
        gridLines.Clear();
    }

    public Color GetColorForInputType(EnemyInputType type)
    {
        return type switch
        {
            EnemyInputType.Any => colorAny,
            EnemyInputType.LeftOnly => colorLeft,
            EnemyInputType.RightOnly => colorRight,
            EnemyInputType.Both => colorBoth,
            EnemyInputType.Spam => colorSpam,
            _ => Color.white
        };
    }
}
