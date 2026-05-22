using UnityEngine;
using UnityEngine.UI;
using TMPro;







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

    [Header("=== Marqueur de Fin ===")]
    [Tooltip("Bouton pour placer la fin de la map au beat actuel")]
    public Button setEndBeatButton;

    [Tooltip("Bouton pour effacer le marqueur de fin")]
    public Button clearEndBeatButton;

    [Tooltip("Texte affiché sur le bouton pour indiquer si un end beat est défini")]
    public TextMeshProUGUI endBeatStatusText;

    [Header("=== Navigation ===")]
    [Tooltip("Bouton pour revenir au menu principal")]
    public Button backToMenuButton;

    private void Start()
    {
        
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

        
        if (metronomeToggle != null)
        {
            metronomeToggle.onValueChanged.AddListener(OnMetronomeToggled);
        }

        
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(OnBackToMenu);
        }

        // Marqueur de fin
        if (setEndBeatButton != null)
            setEndBeatButton.onClick.AddListener(() => BeatMapEditor.Instance?.SetEndBeatAtCursor());
        if (clearEndBeatButton != null)
            clearEndBeatButton.onClick.AddListener(() => BeatMapEditor.Instance?.ClearEndBeat());
    }

    private void Update()
    {
        if (BeatMapEditor.Instance == null) return;

        
        if (!BeatMapEditor.Instance.IsPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W))
                OnMoveBeat(1f);
            if (Input.GetKeyDown(KeyCode.S))
                OnMoveBeat(-1f);
            if (Input.GetKeyDown(KeyCode.D))
                BeatMapEditor.Instance.MoveLane(1);
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.A))
                BeatMapEditor.Instance.MoveLane(-1);
        }

        
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

        
        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            BeatMapEditor.Instance.DeleteNoteAtCursor();

        // E = poser/effacer le marqueur de fin
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (BeatMapEditor.Instance.HasEndBeat)
                BeatMapEditor.Instance.ClearEndBeat();
            else
                BeatMapEditor.Instance.SetEndBeatAtCursor();
        }

        
        if (Input.GetKeyDown(KeyCode.P))
            OnPlayPause();

        
        if (playPauseText != null)
            playPauseText.text = BeatMapEditor.Instance.IsPlaying ? "⏸" : "▶";

        // Afficher le statut du marqueur de fin
        if (endBeatStatusText != null)
        {
            if (BeatMapEditor.Instance.HasEndBeat)
                endBeatStatusText.text = $"Fin: beat {Mathf.RoundToInt(BeatMapEditor.Instance.EndBeat)}";
            else
                endBeatStatusText.text = "Fin: non définie";
        }
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
