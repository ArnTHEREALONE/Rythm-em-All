using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel d'options : Volume (master, musique, SFX) via FMOD Bus.
/// Plus de bouton "ouvrir dossier musique".
/// </summary>
public class OptionsUI : MonoBehaviour
{
    [Header("=== Volume Sliders ===")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    private void Start()
    {
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

        // Appliquer les volumes sauvegardés
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

    /// <summary>
    /// Applique les volumes via FMOD Bus.
    /// </summary>
    private void ApplyVolumes()
    {
        if (FMODAudioManager.Instance != null)
        {
            FMODAudioManager.Instance.SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
            FMODAudioManager.Instance.SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
            FMODAudioManager.Instance.SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
        }
    }
}
