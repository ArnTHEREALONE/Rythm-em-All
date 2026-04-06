using UnityEngine;

/// <summary>
/// Métronome pour l'éditeur de beatmap.
/// Joue un son de clic sur chaque beat si activé.
/// </summary>
public class EditorMetronome : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Son du clic de métronome")]
    public AudioClip metronomeClick;

    [Tooltip("Son du clic sur les temps forts (premier beat de chaque mesure)")]
    public AudioClip metronomeAccent;

    [Tooltip("Nombre de beats par mesure (4 pour du 4/4)")]
    public int beatsPerMeasure = 4;

    [Tooltip("Le métronome est-il activé ?")]
    public bool isEnabled = false;

    [Header("=== Volume ===")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    private AudioSource audioSource;
    private int lastPlayedBeat = -1;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

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
    /// Joue le son de clic.
    /// </summary>
    private void PlayClick(int beatNumber)
    {
        AudioClip clip;

        // Temps fort (premier beat de la mesure)
        if (beatNumber % beatsPerMeasure == 0 && metronomeAccent != null)
            clip = metronomeAccent;
        else
            clip = metronomeClick;

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// Reset le compteur (quand on seek dans la timeline).
    /// </summary>
    public void Reset()
    {
        lastPlayedBeat = -1;
    }
}
