using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel d'options : Volume (master, musique, SFX), inputs, dossier musiques.
/// </summary>
public class OptionsUI : MonoBehaviour
{
    [Header("=== Volume Sliders ===")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("=== Boutons ===")]
    public Button openMusicFolderButton;

    private void Start()
    {
        // Charger les valeurs sauvegardées
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (openMusicFolderButton != null)
            openMusicFolderButton.onClick.AddListener(OnOpenMusicFolder);

        // Appliquer les volumes
        ApplyVolumes();
    }

    private void OnMasterVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        ApplyVolumes();
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        ApplyVolumes();
    }

    private void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateVolumes(
                PlayerPrefs.GetFloat("MasterVolume", 1f),
                PlayerPrefs.GetFloat("MusicVolume", 1f),
                PlayerPrefs.GetFloat("SFXVolume", 1f)
            );
        }
    }

    private void OnOpenMusicFolder()
    {
        string musicPath = System.IO.Path.Combine(Application.persistentDataPath, "Music");

        // Créer le dossier s'il n'existe pas
        if (!System.IO.Directory.Exists(musicPath))
            System.IO.Directory.CreateDirectory(musicPath);

        // Ouvrir le dossier dans l'explorateur
        Application.OpenURL("file:///" + musicPath.Replace("\\", "/"));
    }
}
