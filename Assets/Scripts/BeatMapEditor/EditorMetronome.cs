using UnityEngine;
using FMODUnity;

/// <summary>
/// Métronome pour l'éditeur de beatmap.
/// Joue des sons FMOD one-shot sur chaque beat.
/// </summary>
public class EditorMetronome : MonoBehaviour
{
    [Header("=== SFX FMOD ===")]
    [Tooltip("Event FMOD pour le clic de métronome")]
    public EventReference metronomeClick;

    [Tooltip("Event FMOD pour le clic accentué (temps fort)")]
    public EventReference metronomeAccent;

    [Header("=== Configuration ===")]
    [Tooltip("Nombre de beats par mesure (4 pour du 4/4)")]
    public int beatsPerMeasure = 4;

    [Tooltip("Le métronome est-il activé ?")]
    public bool isEnabled = false;

    private int lastPlayedBeat = -1;

    /// <summary>
    /// Mise à jour du beat actuel. Appelé par BeatMapEditor.
    /// </summary>
    public void UpdateBeat(float currentBeat)
    {
        if (!isEnabled) return;

        int beatInt = Mathf.FloorToInt(currentBeat);

        if (beatInt > lastPlayedBeat)
        {
            lastPlayedBeat = beatInt;
            PlayClick(beatInt);
        }
    }

    /// <summary>
    /// Joue le son de clic via FMOD.
    /// </summary>
    private void PlayClick(int beatNumber)
    {
        EventReference clip;

        // Temps fort (premier beat de la mesure)
        if (beatNumber % beatsPerMeasure == 0 && !metronomeAccent.IsNull)
            clip = metronomeAccent;
        else
            clip = metronomeClick;

        if (!clip.IsNull)
        {
            RuntimeManager.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Reset le compteur (quand on seek dans la timeline).
    /// </summary>
    public void ResetBeat()
    {
        lastPlayedBeat = -1;
    }
}
