using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// Panneau de gestion (Save / Load) de l'éditeur de beatmap.
/// Contient deux champs texte + boutons pour sauvegarder et charger
/// des beatmaps par nom.
/// Ce panneau coulisse depuis le bord gauche de l'écran via EditorSlidingPanel.
/// </summary>
public class EditorGestionPanel : MonoBehaviour
{
    [Header("=== Save ===")]
    [Tooltip("Champ texte pour le nom de la beatmap à sauvegarder")]
    public TMP_InputField saveNameInput;

    [Tooltip("Bouton pour déclencher la sauvegarde")]
    public Button saveButton;

    [Header("=== Load ===")]
    [Tooltip("Champ texte pour le nom de la beatmap à charger")]
    public TMP_InputField loadNameInput;

    [Tooltip("Bouton pour déclencher le chargement")]
    public Button loadButton;

    [Header("=== Feedback ===")]
    [Tooltip("Texte de statut en bas du panneau (ex: 'Sauvegardé !', 'Fichier introuvable')")]
    public TextMeshProUGUI statusText;

    private void Start()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);

        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadClicked);

        // Préremplir le champ save avec le nom actuel
        if (saveNameInput != null && BeatMapEditor.Instance?.CurrentMap != null)
        {
            string currentMapName = BeatMapEditor.Instance.CurrentMap.mapName;
            saveNameInput.text = string.IsNullOrEmpty(currentMapName) ? "Map sans nom" : currentMapName;
        }

        ClearStatus();
    }

    /// <summary>
    /// Sauvegarde la beatmap avec le nom entré dans le champ Save.
    /// </summary>
    private void OnSaveClicked()
    {
        if (BeatMapEditor.Instance == null) return;

        string mapName = saveNameInput != null ? saveNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(mapName))
        {
            SetStatus("⚠ Entrez un nom pour la beatmap.", Color.yellow);
            return;
        }

        BeatMapEditor.Instance.SaveMapWithName(mapName);
        SetStatus($"✓ Beatmap '{mapName}' sauvegardée !", new Color(0f, 1f, 0.53f));
    }

    /// <summary>
    /// Charge une beatmap par le nom entré dans le champ Load.
    /// </summary>
    private void OnLoadClicked()
    {
        if (BeatMapEditor.Instance == null) return;

        string mapName = loadNameInput != null ? loadNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(mapName))
        {
            SetStatus("⚠ Entrez le nom de la beatmap à charger.", Color.yellow);
            return;
        }

        string dir = Path.Combine(Application.persistentDataPath, "BeatMaps");
        string safeName = mapName.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
        string filePath = Path.Combine(dir, safeName + ".json");

        if (!File.Exists(filePath))
        {
            SetStatus($"✕ Fichier introuvable : {safeName}.json", new Color(1f, 0.27f, 0.27f));
            return;
        }

        BeatMapEditor.Instance.LoadMap(filePath);
        SetStatus($"✓ Beatmap '{mapName}' chargée !", new Color(0f, 1f, 0.53f));

        // Mettre à jour le champ save avec le nom chargé
        if (saveNameInput != null)
            saveNameInput.text = mapName;
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }

    private void ClearStatus()
    {
        if (statusText != null)
            statusText.text = "";
    }
}
