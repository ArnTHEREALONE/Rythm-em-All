using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;
using System.Collections.Generic;






public class MapSelectUI : MonoBehaviour
{
    [Header("=== Références ScrollView ===")]
    [Tooltip("Le Transform 'Content' du ScrollView (le parent qui contient les items)")]
    public Transform mapListContent;

    [Header("=== Prefab ===")]
    [Tooltip("Prefab d'une carte de map (si null, les cartes seront créées dynamiquement par code)")]
    public GameObject mapCardPrefab;

    [Header("=== Bouton Retour ===")]
    public Button backButton;

    [Header("=== Couleurs des cartes ===")]
    public Color cardColor = new Color(0.12f, 0.12f, 0.22f, 0.95f);
    public Color cardHoverColor = new Color(0.18f, 0.18f, 0.32f, 1f);
    public Color playButtonColor = new Color(0.2f, 0.75f, 0.4f, 1f);

    
    private List<BeatMapData> loadedMaps = new List<BeatMapData>();

    private void OnEnable()
    {
        
        RefreshMapList();
    }

    
    
    
    public void RefreshMapList()
    {
        
        if (mapListContent != null)
        {
            foreach (Transform child in mapListContent)
                Destroy(child.gameObject);
        }

        loadedMaps.Clear();

        
        string beatMapDir = Path.Combine(Application.persistentDataPath, "BeatMaps");
        if (!Directory.Exists(beatMapDir))
        {
            Directory.CreateDirectory(beatMapDir);
            Debug.Log($"MapSelectUI: Dossier BeatMaps créé dans : {beatMapDir}");
            return;
        }

        
        string[] files = Directory.GetFiles(beatMapDir, "*.json");

        if (files.Length == 0)
        {
            Debug.Log("MapSelectUI: Aucune beatmap trouvée. Crée des maps dans l'éditeur !");
            CreateEmptyMessage();
            return;
        }

        
        foreach (string file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                BeatMapData map = BeatMapData.FromJson(json);

                if (map != null)
                {
                    loadedMaps.Add(map);
                    CreateMapCard(map);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MapSelectUI: Erreur en chargeant {file}: {e.Message}");
            }
        }

        Debug.Log($"MapSelectUI: {loadedMaps.Count} beatmaps chargées.");
    }

    
    
    
    
    private void CreateMapCard(BeatMapData map)
    {
        if (mapListContent == null) return;

        
        string cardObjName = string.IsNullOrEmpty(map.mapName) ? "Map_sans_nom" : map.mapName;
        GameObject card = new GameObject($"MapCard_{cardObjName}");
        card.transform.SetParent(mapListContent, false);

        
        RectTransform cardRect = card.AddComponent<RectTransform>();
        LayoutElement layoutElem = card.AddComponent<LayoutElement>();
        layoutElem.minHeight = 90;
        layoutElem.preferredHeight = 90;
        layoutElem.flexibleWidth = 1;

        
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = cardColor;

        
        HorizontalLayoutGroup hLayout = card.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(15, 15, 10, 10);
        hLayout.spacing = 10;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = true;
        hLayout.childForceExpandHeight = false;

        
        GameObject textZone = new GameObject("TextZone");
        textZone.transform.SetParent(card.transform, false);
        RectTransform textRect = textZone.AddComponent<RectTransform>();

        LayoutElement textLayout = textZone.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1;

        VerticalLayoutGroup vLayout = textZone.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 2;
        vLayout.childAlignment = TextAnchor.MiddleLeft;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        
        string displayMapName = string.IsNullOrEmpty(map.mapName) ? "Map sans nom" : map.mapName;
        CreateTextLine(textZone.transform, displayMapName, 22, Color.white, FontStyles.Bold);

        
        string musicName = !string.IsNullOrEmpty(map.songName) ? map.songName : ExtractMusicName(map.fmodEventPath);
        CreateTextLine(textZone.transform, $"♪ {musicName}", 16, new Color(0.7f, 0.7f, 0.85f));

        
        int highscore = LoadHighScore(displayMapName);
        string details = $"{map.bpm} BPM  •  {map.notes?.Count ?? 0} notes";
        if (highscore > 0)
            details += $"  •  HS: {highscore:N0}";
        CreateTextLine(textZone.transform, details, 14, new Color(0.5f, 0.5f, 0.6f));

        
        GameObject playBtnGO = new GameObject("PlayButton");
        playBtnGO.transform.SetParent(card.transform, false);

        LayoutElement btnLayout = playBtnGO.AddComponent<LayoutElement>();
        btnLayout.minWidth = 80;
        btnLayout.preferredWidth = 80;
        btnLayout.minHeight = 50;

        Image btnImage = playBtnGO.AddComponent<Image>();
        btnImage.color = playButtonColor;

        Button playBtn = playBtnGO.AddComponent<Button>();

        
        GameObject btnTextGO = new GameObject("BtnText");
        btnTextGO.transform.SetParent(playBtnGO.transform, false);
        TextMeshProUGUI btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnText.text = "Go !";
        btnText.fontSize = 28;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        RectTransform btnTextRect = btnTextGO.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        
        BeatMapData capturedMap = map;
        playBtn.onClick.AddListener(() => LaunchMap(capturedMap));

        
        ColorBlock colors = playBtn.colors;
        colors.normalColor = playButtonColor;
        colors.highlightedColor = new Color(0.25f, 0.85f, 0.5f, 1f);
        colors.pressedColor = new Color(0.15f, 0.6f, 0.35f, 1f);
        playBtn.colors = colors;
    }

    
    
    
    private void CreateTextLine(Transform parent, string text, float fontSize, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 6;
    }

    
    
    
    private string ExtractMusicName(string fmodEventPath)
    {
        if (string.IsNullOrEmpty(fmodEventPath)) return "Aucune musique";
        int lastSlash = fmodEventPath.LastIndexOf('/');
        return lastSlash >= 0 ? fmodEventPath.Substring(lastSlash + 1) : fmodEventPath;
    }

    
    
    
    public static int GetHighScore(string mapName)
    {
        if (string.IsNullOrEmpty(mapName)) return 0;
        string key = $"HighScore_{mapName.Replace(" ", "_")}";
        return PlayerPrefs.GetInt(key, 0);
    }

    private int LoadHighScore(string mapName) => GetHighScore(mapName);

    
    
    
    public static void SaveHighScore(string mapName, int score)
    {
        string key = $"HighScore_{mapName.Replace(" ", "_")}";
        int current = PlayerPrefs.GetInt(key, 0);
        if (score > current)
        {
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
        }
    }

    
    
    
    private void LaunchMap(BeatMapData map)
    {
        GameManager.SelectedBeatMap = map;
        SceneManager.LoadScene("Game");
    }

    
    
    
    private void CreateEmptyMessage()
    {
        if (mapListContent == null) return;

        GameObject msgGO = new GameObject("EmptyMessage");
        msgGO.transform.SetParent(mapListContent, false);

        TextMeshProUGUI tmp = msgGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Aucune beatmap trouvée.\nCrée une map dans l'éditeur !";
        tmp.fontSize = 24;
        tmp.color = new Color(0.5f, 0.5f, 0.6f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Italic;

        LayoutElement le = msgGO.AddComponent<LayoutElement>();
        le.preferredHeight = 100;
        le.flexibleWidth = 1;
    }
}
