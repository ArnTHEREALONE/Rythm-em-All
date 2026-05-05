using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Composant attaché dynamiquement à chaque visuel de note dans la grille.
/// Permet le drag & drop d'une note tout en la snappant sur les lanes et beats.
/// </summary>
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

        // Mettre le visuel au premier plan
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || grid == null || grid.gridArea == null) return;

        // Convertir la position de la souris en position locale dans la gridArea
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            grid.gridArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        // Snapper sur la grille
        var (lane, beat) = grid.ScreenToGrid(localPoint);

        // Obtenir les bornes de la vue actuelle pour repositionner
        float currentBeat = BeatMapEditor.Instance.CurrentBeat;
        float startBeat = currentBeat - grid.visibleBeats / 4f;
        float endBeat = currentBeat + grid.visibleBeats * 3f / 4f;

        // Afficher à la position snappée
        grid.PositionInGrid(gameObject, lane, beat, startBeat, endBeat);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || grid == null || grid.gridArea == null) return;
        isDragging = false;

        // Position finale
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            grid.gridArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        var (lane, beat) = grid.ScreenToGrid(localPoint);

        if (BeatMapEditor.Instance != null && note != null)
        {
            // Supprimer l'ancienne note et placer la nouvelle
            EnemyInputType type = note.inputType;
            BeatMapEditor.Instance.DeleteNote(note);
            BeatMapEditor.Instance.PlaceNoteAt(beat, lane, type);
        }
    }
}
