using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Liste des musiques disponibles depuis le MusicDatabase ScriptableObject.
/// Chaque musique est affichée comme un mini-panneau avec :
/// nom, BPM, durée (récupérée dynamiquement via FMOD), et bouton "Sélectionner".
/// Ce panneau coulisse depuis le bord droit via EditorSlidingPanel.
/// </summary>
public class EditorMusicLibrary : MonoBehaviour
{
    [Header("=== Références ===")]
    [Tooltip("Le Transform parent (Content) du ScrollView où les items sont instanciés")]
    public Transform listContent;

    [Tooltip("Prefab pour un item de musique (optionnel — si null, créé dynamiquement)")]
    public GameObject musicItemPrefab;

    [Header("=== Base de données ===")]
    [Tooltip("Référence au MusicDatabase SO contenant toutes les musiques")]
    public MusicDatabase musicDatabase;

    [Header("=== Couleurs ===")]
    public Color cardBackground = new Color(0.12f, 0.16f, 0.25f, 0.95f);
    public Color cardBackgroundSelected = new Color(0.2f, 0.3f, 0.5f, 0.95f);
    public Color selectButtonColor = new Color(0.13f, 0.59f, 0.95f, 1f);
    public Color textColor = new Color(0.9f, 0.9f, 0.95f, 1f);
    public Color subtitleColor = new Color(0.6f, 0.65f, 0.75f, 1f);

    private List<MusicItemUI> musicItems = new List<MusicItemUI>();
    private MusicEntry selectedEntry;

    private class MusicItemUI
    {
        public GameObject root;
        public Image background;
        public MusicEntry entry;
    }

    private void Start()
    {
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

        musicItems.Clear();
        selectedEntry = null;

        if (musicDatabase == null || musicDatabase.entries == null)
        {
            Debug.LogWarning("EditorMusicLibrary: No MusicDatabase assigned!");
            return;
        }

        // Créer un item pour chaque entrée
        for (int i = 0; i < musicDatabase.entries.Count; i++)
        {
            MusicEntry entry = musicDatabase.entries[i];
            CreateMusicCard(entry, i);
        }
    }

    /// <summary>
    /// Crée un mini-panneau (carte) pour une entrée musicale.
    /// Affiche : nom, BPM, durée, bouton "Sélectionner".
    /// </summary>
    private void CreateMusicCard(MusicEntry entry, int index)
    {
        if (listContent == null) return;

        // === Card Root ===
        GameObject card = new GameObject($"MusicCard_{entry.displayName}");
        card.transform.SetParent(listContent, false);

        var layout = card.AddComponent<LayoutElement>();
        layout.minHeight = 110;
        layout.preferredHeight = 110;
        layout.flexibleWidth = 1;

        var cardImage = card.AddComponent<Image>();
        cardImage.color = cardBackground;

        var vertLayout = card.AddComponent<VerticalLayoutGroup>();
        vertLayout.padding = new RectOffset(12, 12, 8, 8);
        vertLayout.spacing = 4;
        vertLayout.childForceExpandWidth = true;
        vertLayout.childForceExpandHeight = false;

        // === Nom de la musique ===
        CreateTextElement(card.transform, entry.displayName, 20, FontStyles.Bold, textColor, 28);

        // === Ligne BPM ===
        CreateTextElement(card.transform, $"BPM: {entry.bpm}", 15, FontStyles.Normal, subtitleColor, 22);

        // === Ligne Durée (récupérée dynamiquement, placeholder pour l'instant) ===
        var durationText = CreateTextElement(card.transform, "Durée: --:--", 15, FontStyles.Normal, subtitleColor, 22);

        // La durée sera mise à jour si FMOD peut la fournir
        // (nécessite de charger temporairement l'event pour obtenir la longueur)
        UpdateDurationText(entry, durationText);

        // === Bouton Sélectionner ===
        GameObject btnGO = new GameObject("SelectButton");
        btnGO.transform.SetParent(card.transform, false);

        var btnLayout = btnGO.AddComponent<LayoutElement>();
        btnLayout.preferredHeight = 32;

        var btnImage = btnGO.AddComponent<Image>();
        btnImage.color = selectButtonColor;

        var btn = btnGO.AddComponent<Button>();
        var btnColors = btn.colors;
        btnColors.highlightedColor = new Color(0.2f, 0.7f, 1f, 1f);
        btnColors.pressedColor = new Color(0.1f, 0.4f, 0.8f, 1f);
        btn.colors = btnColors;

        // Texte du bouton
        var btnTextGO = new GameObject("Text");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        var btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnText.text = "Sélectionner";
        btnText.fontSize = 16;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        var btnTextRect = btnTextGO.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        // Listener
        MusicEntry capturedEntry = entry;
        btn.onClick.AddListener(() => OnSelectClicked(capturedEntry));

        // Track
        var item = new MusicItemUI
        {
            root = card,
            background = cardImage,
            entry = entry
        };
        musicItems.Add(item);
    }

    private TextMeshProUGUI CreateTextElement(Transform parent, string text, int fontSize, FontStyles style, Color color, float height)
    {
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(parent, false);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        var layout = textGO.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        return tmp;
    }

    /// <summary>
    /// Tente de récupérer la durée de la musique via FMOD et met à jour le texte.
    /// </summary>
    private void UpdateDurationText(MusicEntry entry, TextMeshProUGUI durationText)
    {
        if (durationText == null) return;

        // On tente de récupérer la durée depuis l'EventDescription FMOD
        // sans avoir besoin de créer une instance complète
        try
        {
            if (!entry.fmodEvent.IsNull)
            {
                var eventDesc = FMODUnity.RuntimeManager.GetEventDescription(entry.fmodEvent);
                if (eventDesc.isValid())
                {
                    eventDesc.getLength(out int lengthMs);
                    if (lengthMs > 0)
                    {
                        int totalSec = lengthMs / 1000;
                        int min = totalSec / 60;
                        int sec = totalSec % 60;
                        durationText.text = $"Durée: {min:D2}:{sec:D2}";
                    }
                }
            }
        }
        catch (System.Exception)
        {
            // Si FMOD n'est pas prêt, garder le placeholder
            durationText.text = "Durée: --:--";
        }
    }

    /// <summary>
    /// Appelé quand l'utilisateur clique "Sélectionner" sur une musique.
    /// Sélectionne la musique et crée une timeline vide.
    /// </summary>
    private void OnSelectClicked(MusicEntry entry)
    {
        if (entry == null) return;

        selectedEntry = entry;

        // Highlight visuel
        foreach (var item in musicItems)
        {
            item.background.color = (item.entry == entry) ? cardBackgroundSelected : cardBackground;
        }

        // Appliquer la sélection dans BeatMapEditor
        if (BeatMapEditor.Instance != null)
        {
            BeatMapEditor.Instance.SelectMusic(entry);
        }

        Debug.Log($"EditorMusicLibrary: Selected '{entry.displayName}' ({entry.bpm} BPM)");
    }
}
