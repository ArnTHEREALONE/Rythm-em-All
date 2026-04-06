using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Liste des musiques enregistrées dans persistentDataPath/Music/.
/// Menu scrollable pour sélectionner une musique dans l'éditeur.
/// </summary>
public class EditorMusicLibrary : MonoBehaviour
{
    [Header("=== Références ===")]
    public Transform listContent;
    public GameObject musicItemPrefab;
    public Button selectButton;
    public Button importButton;
    public TextMeshProUGUI selectedMusicText;

    private List<string> musicFiles = new List<string>();
    private string selectedFile;

    private void Start()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectClicked);
            selectButton.interactable = false;
        }

        if (importButton != null)
        {
            importButton.onClick.AddListener(OnImportClicked);
        }

        RefreshList();
    }

    /// <summary>
    /// Rafraîchit la liste des fichiers musique.
    /// </summary>
    public void RefreshList()
    {
        // Nettoyer
        if (listContent != null)
        {
            foreach (Transform child in listContent)
                Destroy(child.gameObject);
        }

        musicFiles.Clear();
        selectedFile = null;
        if (selectButton != null)
            selectButton.interactable = false;

        // Scanner le dossier
        string musicDir = Path.Combine(Application.persistentDataPath, "Music");
        if (!Directory.Exists(musicDir))
        {
            Directory.CreateDirectory(musicDir);
            return;
        }

        string[] extensions = { "*.ogg", "*.wav", "*.mp3" };
        foreach (string ext in extensions)
        {
            string[] files = Directory.GetFiles(musicDir, ext);
            foreach (string file in files)
            {
                musicFiles.Add(file);
                CreateMusicItem(file);
            }
        }
    }

    /// <summary>
    /// Crée un item dans la liste pour un fichier musique.
    /// </summary>
    private void CreateMusicItem(string filePath)
    {
        if (listContent == null) return;

        string fileName = Path.GetFileName(filePath);

        GameObject item;
        if (musicItemPrefab != null)
        {
            item = Instantiate(musicItemPrefab, listContent);
        }
        else
        {
            item = new GameObject($"Music_{fileName}");
            item.transform.SetParent(listContent, false);

            var layout = item.AddComponent<LayoutElement>();
            layout.minHeight = 50;
            layout.preferredHeight = 50;

            var button = item.AddComponent<Button>();
            var image = item.AddComponent<Image>();
            image.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(item.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = Path.GetFileNameWithoutExtension(fileName);
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.margin = new Vector4(10, 0, 10, 0);

            var rectText = textGO.GetComponent<RectTransform>();
            rectText.anchorMin = Vector2.zero;
            rectText.anchorMax = Vector2.one;
            rectText.offsetMin = Vector2.zero;
            rectText.offsetMax = Vector2.zero;
        }

        Button btn = item.GetComponent<Button>();
        if (btn != null)
        {
            string capturedFile = fileName;
            btn.onClick.AddListener(() => SelectFile(capturedFile));
        }
    }

    private void SelectFile(string fileName)
    {
        selectedFile = fileName;

        if (selectedMusicText != null)
            selectedMusicText.text = Path.GetFileNameWithoutExtension(fileName);

        if (selectButton != null)
            selectButton.interactable = true;
    }

    private void OnSelectClicked()
    {
        if (string.IsNullOrEmpty(selectedFile)) return;

        if (BeatMapEditor.Instance != null)
        {
            BeatMapEditor.Instance.SelectMusic(selectedFile);
        }

        // Fermer le panel
        gameObject.SetActive(false);
    }

    private void OnImportClicked()
    {
        // Ouvrir le dossier pour que l'utilisateur y mette ses fichiers
        EditorMusicImporter importer = BeatMapEditor.Instance?.musicImporter;
        if (importer != null)
        {
            importer.OpenMusicFolder();
        }
    }
}
