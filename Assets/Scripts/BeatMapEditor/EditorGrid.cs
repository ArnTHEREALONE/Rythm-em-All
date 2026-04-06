using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Grille visuelle de l'éditeur : colonnes = lanes (spawners), lignes = beats.
/// Affiche les notes placées et permet le placement via navigation ZQSD.
/// </summary>
public class EditorGrid : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Nombre de lanes (= nombre de spawners)")]
    public int laneCount = 8;

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

    private List<GameObject> noteVisuals = new List<GameObject>();
    private GameObject cursorVisual;

    private void Start()
    {
        CreateCursor();
    }

    /// <summary>
    /// Rafraîchit l'affichage de la grille.
    /// </summary>
    public void Refresh()
    {
        ClearNotes();

        if (BeatMapEditor.Instance == null) return;

        var map = BeatMapEditor.Instance.CurrentMap;
        if (map == null || map.notes == null) return;

        float currentBeat = BeatMapEditor.Instance.CurrentBeat;
        float startBeat = currentBeat - visibleBeats / 4f;
        float endBeat = currentBeat + visibleBeats * 3f / 4f;

        // Afficher les notes dans la zone visible
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

        // Mettre à jour le curseur
        UpdateCursor(currentBeat, startBeat, endBeat);
    }

    /// <summary>
    /// Crée un visuel pour une note dans la grille.
    /// </summary>
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
            rect.sizeDelta = new Vector2(
                gridArea.rect.width / laneCount * 0.8f,
                gridArea.rect.height / visibleBeats * 0.6f
            );
        }

        // Positionner le visuel
        PositionInGrid(noteGO, note.spawnerIndex, note.beatTime, startBeat, endBeat);

        // Appliquer la couleur
        Image image = noteGO.GetComponent<Image>();
        if (image != null)
            image.color = GetColorForInputType(note.inputType);

        noteVisuals.Add(noteGO);
    }

    /// <summary>
    /// Position un GameObject dans la grille.
    /// </summary>
    private void PositionInGrid(GameObject go, int lane, float beat, float startBeat, float endBeat)
    {
        if (gridArea == null) return;

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) return;

        float gridWidth = gridArea.rect.width;
        float gridHeight = gridArea.rect.height;

        // X : position dans la lane (gauche à droite)
        float laneWidth = gridWidth / laneCount;
        float x = (lane + 0.5f) * laneWidth - gridWidth / 2f;

        // Y : position temporelle (haut = futur, bas = passé)
        float beatRange = endBeat - startBeat;
        float normalizedBeat = (beat - startBeat) / beatRange;
        float y = (normalizedBeat - 0.5f) * gridHeight;

        rect.anchoredPosition = new Vector2(x, y);
    }

    /// <summary>
    /// Met à jour la position du curseur.
    /// </summary>
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

    /// <summary>
    /// Retourne la couleur associée à un type d'input.
    /// </summary>
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
