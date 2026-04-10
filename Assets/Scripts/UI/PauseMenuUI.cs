using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    public GameObject pausePanel;
    public Button resumeButton;
    public Button restartButton;
    public Button quitToMenuButton;

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResume);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestart);
        if (quitToMenuButton != null)
            quitToMenuButton.onClick.AddListener(OnQuitToMenu);
    }

    private void OnResume()
    {
        GameManager.Instance?.TogglePause();
    }

    private void OnRestart()
    {
        GameManager.Instance?.RestartGame();
    }

    private void OnQuitToMenu()
    {
        GameManager.Instance?.ReturnToMenu();
    }
}
