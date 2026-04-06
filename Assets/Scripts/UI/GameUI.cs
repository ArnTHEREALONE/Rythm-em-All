using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD en jeu : Score, Combo, Multiplicateur, Vitesse, PV.
/// S'abonne aux events des managers pour se mettre à jour.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("=== Textes ===")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI speedText;

    [Header("=== Slider PV ===")]
    public Slider hpSlider;

    [Header("=== Panels ===")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("=== Game Over / Victory ===")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalComboText;

    [Header("=== Boutons Pause ===")]
    public Button resumeButton;
    public Button restartButton;
    public Button quitButton;

    [Header("=== Boutons Game Over / Victory ===")]
    public Button restartButton2;
    public Button quitButton2;

    private void Start()
    {
        // Cacher les panels
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        // S'abonner aux events
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

        // Boutons
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

        // Valeurs initiales
        UpdateScore(0);
        UpdateCombo(0);
        UpdateMultiplier(1f);
        UpdateSpeed(1f);
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // Gérer l'affichage des panels selon l'état du jeu
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
            // Animation de scale pour les gros combos
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
        // Se désabonner
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
