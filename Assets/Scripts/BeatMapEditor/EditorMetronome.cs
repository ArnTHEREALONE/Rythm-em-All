using UnityEngine;

public class EditorMetronome : MonoBehaviour
{
    public AudioClip metronomeClick;

    public AudioClip metronomeAccent;

    public int beatsPerMeasure = 4;

    public bool isEnabled = false;

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
    private void PlayClick(int beatNumber)
    {
        AudioClip clip;

        if (beatNumber % beatsPerMeasure == 0 && metronomeAccent != null)
            clip = metronomeAccent;
        else
            clip = metronomeClick;

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
    public void Reset()
    {
        lastPlayedBeat = -1;
    }
}
