using UnityEngine;
using UnityEngine.EventSystems;





public class EditorNoteDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private BeatNote note;
    private EditorGrid grid;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private bool isDragging;

    public void Initialize(BeatNote note, EditorGrid grid)
    {
        this.note = note;
        this.grid = grid;
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (note == null || BeatMapEditor.Instance == null || BeatMapEditor.Instance.IsPlaying) return;
        isDragging = true;

        
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || grid == null || grid.gridArea == null) return;

        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            grid.gridArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        
        var (lane, beat) = grid.ScreenToGrid(localPoint);

        
        float currentBeat = BeatMapEditor.Instance.CurrentBeat;
        float startBeat = currentBeat - grid.visibleBeats / 4f;
        float endBeat = currentBeat + grid.visibleBeats * 3f / 4f;

        
        grid.PositionInGrid(gameObject, lane, beat, startBeat, endBeat);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || grid == null || grid.gridArea == null) return;
        isDragging = false;

        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            grid.gridArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        var (lane, beat) = grid.ScreenToGrid(localPoint);

        if (BeatMapEditor.Instance != null && note != null)
        {
            
            EnemyInputType type = note.inputType;
            BeatMapEditor.Instance.DeleteNote(note);
            BeatMapEditor.Instance.PlaceNoteAt(beat, lane, type);
        }
    }
}
