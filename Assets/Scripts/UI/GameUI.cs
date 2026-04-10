using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI speedText;

    public Slider hpSlider;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalComboText;

    public Button resumeButton;
    public Button restartButton;
    public Button quitButton;

    public Button restartButton2;
    public Button quitButton2;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
            ScoreManager.Instance.OnComboChanged += UpdateCombo;
            ScoreManager.Instance.OnMultiplierChanged += UpdateMultiplier;
        }

        if (SpeedMultiplier.Instance != null)
        {
            SpeedMultiplier.Instance.OnSpeedChanged += UpdateSpeed;
        }

        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => GameManager.Instance?.TogglePause());
        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (quitButton != null)
            quitButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());
        if (restartButton2 != null)
            restartButton2.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (quitButton2 != null)
            quitButton2.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());

        UpdateScore(0);
        UpdateCombo(0);
        UpdateMultiplier(1f);
        UpdateSpeed(1f);
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        switch (GameManager.Instance.CurrentState)
        {
            case GameManager.GameState.Paused:
                if (pausePanel != null && !pausePanel.activeSelf)
                    pausePanel.SetActive(true);
                break;

            case GameManager.GameState.Playing:
                if (pausePanel != null && pausePanel.activeSelf)
                    pausePanel.SetActive(false);
                break;

            case GameManager.GameState.GameOver:
                ShowEndScreen(gameOverPanel);
                break;

            case GameManager.GameState.Victory:
                ShowEndScreen(victoryPanel);
                break;
        }
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString("N0");
    }

    private void UpdateCombo(int combo)
    {
        if (comboText != null)
        {
            comboText.text = combo > 0 ? $"{combo}x" : "";
            if (combo > 0 && combo % 10 == 0)
            {
                comboText.transform.localScale = Vector3.one * 1.5f;
            }
            else
            {
                comboText.transform.localScale = Vector3.one;
            }
        }
    }

    private void UpdateMultiplier(float multiplier)
    {
        if (multiplierText != null)
            multiplierText.text = $"×{multiplier:F1}";
    }

    private void UpdateSpeed(float speed)
    {
        if (speedText != null)
            speedText.text = $"Speed: ×{speed:F2}";
    }

    private void ShowEndScreen(GameObject panel)
    {
        if (panel != null && !panel.activeSelf)
        {
            panel.SetActive(true);

            if (finalScoreText != null && ScoreManager.Instance != null)
                finalScoreText.text = $"Score: {ScoreManager.Instance.TotalScore:N0}";
            if (finalComboText != null && ScoreManager.Instance != null)
                finalComboText.text = $"Best Combo: {ScoreManager.Instance.BestCombo}x";
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
            ScoreManager.Instance.OnComboChanged -= UpdateCombo;
            ScoreManager.Instance.OnMultiplierChanged -= UpdateMultiplier;
        }

        if (SpeedMultiplier.Instance != null)
        {
            SpeedMultiplier.Instance.OnSpeedChanged -= UpdateSpeed;
        }
    }
}
