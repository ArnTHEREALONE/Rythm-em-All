using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Liste des musiques disponibles depuis le MusicDatabase ScriptableObject.
/// Plus de scan de fichiers — tout vient du projet FMOD.
/// </summary>
public class EditorMusicLibrary : MonoBehaviour
{
    [Header("=== Références ===")]
    public Transform listContent;
    public GameObject musicItemPrefab;
    public Button selectButton;
    public TextMeshProUGUI selectedMusicText;

    [Header("=== Base de données ===")]
    [Tooltip("Référence au MusicDatabase SO contenant toutes les musiques")]
    public MusicDatabase musicDatabase;

    private MusicEntry selectedEntry;

    private void Start()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectClicked);
            selectButton.interactable = false;
        }

        RefreshList();
    }

    /// <summary>
    /// Rafraîchit la liste depuis le MusicDatabase.
    /// </summary>
    public void RefreshList()
    {
        // Nettoyer
        if (listContent != null)
        {
            foreach (Transform child in listContent)
                Destroy(child.gameObject);
        }

        selectedEntry = null;
        if (selectButton != null)
            selectButton.interactable = false;

        if (musicDatabase == null || musicDatabase.entries == null)
        {
            Debug.LogWarning("EditorMusicLibrary: No MusicDatabase assigned!");
            return;
        }

        // Créer un item pour chaque entrée
        for (int i = 0; i < musicDatabase.entries.Count; i++)
        {
            MusicEntry entry = musicDatabase.entries[i];
            CreateMusicItem(entry, i);
        }
    }

    /// <summary>
    /// Crée un item dans la liste pour une entrée musicale.
    /// </summary>
    private void CreateMusicItem(MusicEntry entry, int index)
    {
        if (listContent == null) return;

        GameObject item;
        if (musicItemPrefab != null)
        {
            item = Instantiate(musicItemPrefab, listContent);
        }
        else
        {
            item = new GameObject($"Music_{entry.displayName}");
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
            text.text = $"{entry.displayName}  |  {entry.bpm} BPM";
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
            MusicEntry capturedEntry = entry;
            btn.onClick.AddListener(() => SelectEntry(capturedEntry));
        }
    }

    private void SelectEntry(MusicEntry entry)
    {
        selectedEntry = entry;

        if (selectedMusicText != null)
            selectedMusicText.text = entry.displayName;

        if (selectButton != null)
            selectButton.interactable = true;
    }

    private void OnSelectClicked()
    {
        if (selectedEntry == null) return;

        if (BeatMapEditor.Instance != null)
        {
            BeatMapEditor.Instance.SelectMusic(selectedEntry);
        }

        gameObject.SetActive(false);
    }
}
