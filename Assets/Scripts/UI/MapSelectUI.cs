using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;
using System.Collections.Generic;

public class MapSelectUI : MonoBehaviour
{
    public Transform mapListContent;

    public GameObject mapItemPrefab;

    public TextMeshProUGUI selectedMapName;
    public TextMeshProUGUI selectedMapBPM;
    public TextMeshProUGUI selectedMapNotes;
    public Button playSelectedButton;

    private List<BeatMapData> loadedMaps = new List<BeatMapData>();
    private BeatMapData selectedMap;

    private void Start()
    {
        if (playSelectedButton != null)
        {
            playSelectedButton.onClick.AddListener(OnPlaySelected);
            playSelectedButton.interactable = false;
        }

        string beatMapDir = Path.Combine(Application.persistentDataPath, "BeatMaps");
        if (!Directory.Exists(beatMapDir))
            Directory.CreateDirectory(beatMapDir);
    }


    public void RefreshMapList()
    {
        if (mapListContent != null)
        {
            foreach (Transform child in mapListContent)
                Destroy(child.gameObject);
        }

        loadedMaps.Clear();
        selectedMap = null;
        if (playSelectedButton != null)
            playSelectedButton.interactable = false;

        string beatMapDir = Path.Combine(Application.persistentDataPath, "BeatMaps");
        if (!Directory.Exists(beatMapDir))
        {
            Debug.Log("MapSelectUI: No BeatMaps directory found.");
            return;
        }

        string[] files = Directory.GetFiles(beatMapDir, "*.json");

        foreach (string file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                BeatMapData map = BeatMapData.FromJson(json);

                if (map != null)
                {
                    loadedMaps.Add(map);
                    CreateMapItem(map, file);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MapSelectUI: Error loading {file}: {e.Message}");
            }
        }

        Debug.Log($"MapSelectUI: Loaded {loadedMaps.Count} beatmaps.");
    }

    private void CreateMapItem(BeatMapData map, string filePath)
    {
        if (mapListContent == null) return;

        GameObject item;
        if (mapItemPrefab != null)
        {
            item = Instantiate(mapItemPrefab, mapListContent);
        }
        else
        {
            item = new GameObject($"MapItem_{map.songName}");
            item.transform.SetParent(mapListContent, false);

            var layout = item.AddComponent<LayoutElement>();
            layout.minHeight = 60;
            layout.preferredHeight = 60;

            var button = item.AddComponent<Button>();
            var image = item.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.25f, 0.9f);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(item.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = $"{map.songName}  |  {map.bpm} BPM  |  {map.notes?.Count ?? 0} notes";
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.margin = new Vector4(15, 0, 15, 0);

            var rectText = textGO.GetComponent<RectTransform>();
            rectText.anchorMin = Vector2.zero;
            rectText.anchorMax = Vector2.one;
            rectText.offsetMin = Vector2.zero;
            rectText.offsetMax = Vector2.zero;
        }

        Button btn = item.GetComponent<Button>();
        if (btn != null)
        {
            BeatMapData capturedMap = map;
            btn.onClick.AddListener(() => SelectMap(capturedMap));
        }
    }

    private void SelectMap(BeatMapData map)
    {
        selectedMap = map;

        if (selectedMapName != null)
            selectedMapName.text = map.songName;
        if (selectedMapBPM != null)
            selectedMapBPM.text = $"{map.bpm} BPM";
        if (selectedMapNotes != null)
            selectedMapNotes.text = $"{map.notes?.Count ?? 0} notes";
        if (playSelectedButton != null)
            playSelectedButton.interactable = true;
    }

    private void OnPlaySelected()
    {
        if (selectedMap == null) return;

        GameManager.SelectedBeatMap = selectedMap;
        GameManager.SelectedMusicPath = Path.Combine(
            Application.persistentDataPath, "Music", selectedMap.musicFileName);

        SceneManager.LoadScene("Game");
    }
}
