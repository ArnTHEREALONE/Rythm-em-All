using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class MainMenuUI : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject mapSelectPanel;
    public GameObject optionsPanel;
    public Button playButton;
    public Button editorButton;
    public Button optionsButton;
    public Button quitButton;
    public Button mapSelectBackButton;
    public Button optionsBackButton;

    private void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        if (editorButton != null)
            editorButton.onClick.AddListener(OnEditorClicked);
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        if (mapSelectBackButton != null)
            mapSelectBackButton.onClick.AddListener(OnMapSelectBackClicked);
        if (optionsBackButton != null)
            optionsBackButton.onClick.AddListener(OnOptionsBackClicked);

        ShowMainPanel();
    }

    public void OnPlayClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (mapSelectPanel != null)
        {
            mapSelectPanel.SetActive(true);

            MapSelectUI mapSelect = mapSelectPanel.GetComponent<MapSelectUI>();
            if (mapSelect != null)
                mapSelect.RefreshMapList();
        }
    }

    public void OnEditorClicked()
    {
        SceneManager.LoadScene("Editor");
    }

    public void OnOptionsClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnMapSelectBackClicked()
    {
        ShowMainPanel();
    }

    private void OnOptionsBackClicked()
    {
        ShowMainPanel();
    }

    private void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (mapSelectPanel != null) mapSelectPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }
}
