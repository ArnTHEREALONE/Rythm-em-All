using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Contrôles de transport de l'éditeur : Play/Pause, avancer/reculer par beat,
// BPM input, toggle métronome.
// Navigation gauche/droite/haut/bas pour se déplacer dans la grille.
public class EditorControls : MonoBehaviour
{
    [Header("=== Boutons de transport ===")]
    public Button playPauseButton;
    public Button prevBeatButton;
    public Button nextBeatButton;
    public Button prevHalfBeatButton;
    public Button nextHalfBeatButton;

    [Header("=== BPM ===")]
    public TMP_InputField bpmInputField;

    [Header("=== Métronome ===")]
    public Toggle metronomeToggle;

    [Header("=== Texte Play/Pause ===")]
    public TextMeshProUGUI playPauseText;

    private void Start()
    {
        // Nav
        if (playPauseButton != null)
            playPauseButton.onClick.AddListener(OnPlayPause);
        if (prevBeatButton != null)
            prevBeatButton.onClick.AddListener(() => OnMoveBeat(-1f));
        if (nextBeatButton != null)
            nextBeatButton.onClick.AddListener(() => OnMoveBeat(1f));
        if (prevHalfBeatButton != null)
            prevHalfBeatButton.onClick.AddListener(() => OnMoveBeat(-0.5f));
        if (nextHalfBeatButton != null)
            nextHalfBeatButton.onClick.AddListener(() => OnMoveBeat(0.5f));

        // BPM
        if (bpmInputField != null)
        {
            bpmInputField.onEndEdit.AddListener(OnBPMChanged);

            if (BeatMapEditor.Instance?.CurrentMap != null)
                bpmInputField.text = BeatMapEditor.Instance.CurrentMap.bpm.ToString("F0");
        }

        // Métronome
        if (metronomeToggle != null)
        {
            metronomeToggle.onValueChanged.AddListener(OnMetronomeToggled);
        }
    }

    private void Update()
    {
        if (BeatMapEditor.Instance == null || BeatMapEditor.Instance.IsPlaying) return;

        if (bpmInputField != null && bpmInputField.isFocused) return;

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W))
            OnMoveBeat(1f);
        if (Input.GetKeyDown(KeyCode.S))
            OnMoveBeat(-1f);
        if (Input.GetKeyDown(KeyCode.D))
            BeatMapEditor.Instance.MoveLane(1);
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.A))
            BeatMapEditor.Instance.MoveLane(-1);

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

        if (Input.GetKeyDown(KeyCode.P))
            OnPlayPause();

        if (playPauseText != null)
            playPauseText.text = BeatMapEditor.Instance.IsPlaying ? "II" : "I>";
    }

    private void OnPlayPause()
    {
        BeatMapEditor.Instance?.TogglePlayback();
    }

    private void OnMoveBeat(float delta)
    {
        BeatMapEditor.Instance?.MoveBeat(delta);
    }

    private void OnBPMChanged(string value)
    {
        if (float.TryParse(value, out float bpm) && bpm > 0)
        {
            BeatMapEditor.Instance?.SetBPM(bpm);
        }
    }

    private void OnMetronomeToggled(bool isOn)
    {
        if (BeatMapEditor.Instance?.metronome != null)
        {
            BeatMapEditor.Instance.metronome.isEnabled = isOn;
        }
    }
}
