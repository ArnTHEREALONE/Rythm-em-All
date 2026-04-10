using UnityEngine;
using System;
using System.Collections;
using System.IO;


public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("=== Audio Sources ===")]
    [Tooltip("AudioSource pour la musique de fond")]
    public AudioSource musicSource;

    [Tooltip("AudioSource pour les effets sonores")]
    public AudioSource sfxSource;

    [Header("=== Volumes ===")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Range(0f, 1f)]
    public float musicVolume = 1f;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    public float MusicTime
    {
        get
        {
            if (musicSource != null && musicSource.isPlaying)
                return musicSource.time;
            return 0f;
        }
    }

    public bool IsPlaying => musicSource != null && musicSource.isPlaying;

    public float MusicLength => musicSource != null && musicSource.clip != null ? musicSource.clip.length : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.spatialBlend = 0f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        musicSource.clip = clip;
        musicSource.volume = masterVolume * musicVolume;
        musicSource.Play();
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
            musicSource.UnPause();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void SetPitch(float pitch)
    {
        if (musicSource != null)
            musicSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
    }

    public void SeekTo(float timeInSeconds)
    {
        if (musicSource != null && musicSource.clip != null)
            musicSource.time = Mathf.Clamp(timeInSeconds, 0f, musicSource.clip.length);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip, masterVolume * sfxVolume);
    }

    public void UpdateVolumes(float master, float music, float sfx)
    {
        masterVolume = master;
        musicVolume = music;
        sfxVolume = sfx;

        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;
    }

    public void LoadAudioClip(string filePath, Action<AudioClip> onLoaded)
    {
        StartCoroutine(LoadAudioClipCoroutine(filePath, onLoaded));
    }

    private IEnumerator LoadAudioClipCoroutine(string filePath, Action<AudioClip> onLoaded)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"AudioManager: File not found: {filePath}");
            onLoaded?.Invoke(null);
            yield break;
        }

        string fileUri = "file:///" + filePath.Replace("\\", "/");
        AudioType audioType = GetAudioType(filePath);

        using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(fileUri, audioType))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                clip.name = Path.GetFileNameWithoutExtension(filePath);
                onLoaded?.Invoke(clip);
            }
            else
            {
                Debug.LogError($"AudioManager: Failed to load audio: {www.error}");
                onLoaded?.Invoke(null);
            }
        }
    }

    private AudioType GetAudioType(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        return ext switch
        {
            ".ogg" => AudioType.OGGVORBIS,
            ".wav" => AudioType.WAV,
            ".mp3" => AudioType.MPEG,
            _ => AudioType.UNKNOWN
        };
    }

    private void Update()
    {
        if (SpeedMultiplier.Instance != null && musicSource != null && musicSource.isPlaying)
        {
            musicSource.pitch = SpeedMultiplier.Instance.GetMusicPitch();
        }
    }
}
