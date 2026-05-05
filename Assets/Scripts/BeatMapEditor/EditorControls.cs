using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Contrôles de transport de l'éditeur de beatmap.
/// 5 boutons : -2 beats, -1 beat, play/pause, +1 beat, +2 beats.
/// Gère aussi les raccourcis clavier pour la navigation dans la grille
/// et le placement de notes.
/// </summary>
public class EditorControls : MonoBehaviour
{
    [Header("=== Boutons de transport ===")]
    [Tooltip("Recule de 2 beats")]
    public Button prevTwoBeatsButton;

    [Tooltip("Recule de 1 beat")]
    public Button prevBeatButton;

    [Tooltip("Play / Pause")]
    public Button playPauseButton;

    [Tooltip("Avance de 1 beat")]
    public Button nextBeatButton;

    [Tooltip("Avance de 2 beats")]
    public Button nextTwoBeatsButton;

    [Header("=== Texte Play/Pause ===")]
    public TextMeshProUGUI playPauseText;

    [Header("=== Métronome ===")]
    public Toggle metronomeToggle;

    [Header("=== Retour Menu ===")]
    [Tooltip("Bouton en haut à gauche pour revenir au menu principal")]
    public Button backToMenuButton;

    private void Start()
    {
        // Navigation transport
        if (prevTwoBeatsButton != null)
            prevTwoBeatsButton.onClick.AddListener(() => OnMoveBeat(-2f));
        if (prevBeatButton != null)
            prevBeatButton.onClick.AddListener(() => OnMoveBeat(-1f));
        if (playPauseButton != null)
            playPauseButton.onClick.AddListener(OnPlayPause);
        if (nextBeatButton != null)
            nextBeatButton.onClick.AddListener(() => OnMoveBeat(1f));
        if (nextTwoBeatsButton != null)
            nextTwoBeatsButton.onClick.AddListener(() => OnMoveBeat(2f));

        // Métronome
        if (metronomeToggle != null)
        {
            metronomeToggle.onValueChanged.AddListener(OnMetronomeToggled);
        }

        // Retour menu
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(OnBackToMenu);
        }
    }

    private void Update()
    {
        if (BeatMapEditor.Instance == null) return;

        // Pas de raccourcis clavier pendant la lecture
        if (!BeatMapEditor.Instance.IsPlaying)
        {
            // Navigation grille (ZQSD)
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W))
                OnMoveBeat(1f);
            if (Input.GetKeyDown(KeyCode.S))
                OnMoveBeat(-1f);
            if (Input.GetKeyDown(KeyCode.D))
                BeatMapEditor.Instance.MoveLane(1);
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.A))
                BeatMapEditor.Instance.MoveLane(-1);

            // Placement de notes
            if (Input.GetKeyDown(KeyCode.Space))
                BeatMapEditor.Instance.PlaceNote(EnemyInputType.Any);
            if (Input.GetKeyDown(KeyCode.Alpha1))
                BeatMapEditor.Instance.PlaceNote(EnemyInputType.LeftOnly);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                BeatMapEditor.Instance.PlaceNote(EnemyInputType.RightOnly);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                BeatMapEditor.Instance.PlaceNote(EnemyInputType.Both);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                BeatMapEditor.Instance.PlaceNote(EnemyInputType.Spam);

            // Suppression
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                BeatMapEditor.Instance.DeleteNoteAtCursor();
        }

        // Play/Pause (toujours actif)
        if (Input.GetKeyDown(KeyCode.P))
            OnPlayPause();

        // Mise à jour du texte play/pause
        if (playPauseText != null)
            playPauseText.text = BeatMapEditor.Instance.IsPlaying ? "⏸" : "▶";
    }

    private void OnPlayPause()
    {
        BeatMapEditor.Instance?.TogglePlayback();
    }

    private void OnMoveBeat(float delta)
    {
        BeatMapEditor.Instance?.MoveBeat(delta);
    }

    private void OnMetronomeToggled(bool isOn)
    {
        if (BeatMapEditor.Instance?.metronome != null)
        {
            BeatMapEditor.Instance.metronome.isEnabled = isOn;
        }
    }

    private void OnBackToMenu()
    {
        BeatMapEditor.Instance?.ReturnToMenu();
    }
}
