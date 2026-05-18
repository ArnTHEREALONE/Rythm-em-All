using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameUI : MonoBehaviour
{
    [Header("=== HUD ===")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI speedText;
    public Slider hpSlider;

    [Header("=== Décompte ===")]
    [Tooltip("Texte affiché pour le décompte (3, 2, 1, Go!)")]
    public TextMeshProUGUI countdownText;

    [Header("=== Pause ===")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button restartButton;
    public Button quitButton;

    [Header("=== Victoire ===")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryScoreText;
    public TextMeshProUGUI victoryBestComboText;
    public TextMeshProUGUI victoryHighscoreText;
    public Button victoryRestartButton;
    public Button victoryMenuButton;

    [Header("=== Défaite ===")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverScoreText;
    public TextMeshProUGUI gameOverBestComboText;
    public Button gameOverRestartButton;
    public Button gameOverMenuButton;

    // Rétro-compatibilité avec les anciens boutons
    public Button restartButton2;
    public Button quitButton2;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalComboText;

    private Coroutine countdownAnimCoroutine;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        // Abonnements Score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
            ScoreManager.Instance.OnComboChanged += UpdateCombo;
            ScoreManager.Instance.OnMultiplierChanged += UpdateMultiplier;
        }

        if (SpeedMultiplier.Instance != null)
            SpeedMultiplier.Instance.OnSpeedChanged += UpdateSpeed;

        // Abonnements GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCountdownTick += ShowCountdownTick;
            GameManager.Instance.OnVictory += ShowVictoryScreen;
            GameManager.Instance.OnGameOver += ShowGameOverScreen;
            GameManager.Instance.OnGameStarted += HideCountdown;
        }

        // Boutons Pause
        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => GameManager.Instance?.TogglePause());
        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (quitButton != null)
            quitButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());

        // Boutons Victoire
        if (victoryRestartButton != null)
            victoryRestartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (victoryMenuButton != null)
            victoryMenuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());

        // Boutons Défaite
        if (gameOverRestartButton != null)
            gameOverRestartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (gameOverMenuButton != null)
            gameOverMenuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());

        // Rétro-compat
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
            case GameManager.GameState.Countdown:
                if (pausePanel != null && pausePanel.activeSelf)
                    pausePanel.SetActive(false);
                break;
        }
    }

    // ── Décompte ─────────────────────────────────────────

    private void ShowCountdownTick(string tick)
    {
        if (countdownText == null) return;

        countdownText.gameObject.SetActive(true);
        countdownText.text = tick;

        if (countdownAnimCoroutine != null)
            StopCoroutine(countdownAnimCoroutine);

        countdownAnimCoroutine = StartCoroutine(AnimateCountdownTick());
    }

    private IEnumerator AnimateCountdownTick()
    {
        if (countdownText == null) yield break;

        // Apparaît grand puis rétrécit
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 bigScale = Vector3.one * 2f;
        Vector3 normalScale = Vector3.one;

        countdownText.transform.localScale = bigScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            countdownText.transform.localScale = Vector3.Lerp(bigScale, normalScale, t);
            yield return null;
        }

        countdownText.transform.localScale = normalScale;
    }

    private void HideCountdown()
    {
        if (countdownAnimCoroutine != null)
            StopCoroutine(countdownAnimCoroutine);

        if (countdownText != null)
            StartCoroutine(FadeOutCountdown());
    }

    private IEnumerator FadeOutCountdown()
    {
        if (countdownText == null) yield break;

        // Maintenir "Go!" visible une fraction de seconde puis disparaître
        yield return new WaitForSeconds(0.4f);
        countdownText.gameObject.SetActive(false);
    }

    // ── Victoire ─────────────────────────────────────────

    private void ShowVictoryScreen(int score, bool isNewRecord)
    {
        if (victoryPanel == null) return;

        victoryPanel.SetActive(true);

        if (victoryScoreText != null)
            victoryScoreText.text = $"Score\n{score:N0}";

        if (victoryBestComboText != null && ScoreManager.Instance != null)
            victoryBestComboText.text = $"Best Combo: {ScoreManager.Instance.BestCombo}x";

        if (victoryHighscoreText != null)
        {
            if (isNewRecord)
            {
                victoryHighscoreText.text = "🏆 Nouveau Record !";
                victoryHighscoreText.gameObject.SetActive(true);
            }
            else
            {
                int hs = MapSelectUI.GetHighScore(GameManager.SelectedBeatMap?.mapName ?? "");
                victoryHighscoreText.text = hs > 0 ? $"Record: {hs:N0}" : "";
                victoryHighscoreText.gameObject.SetActive(hs > 0);
            }
        }

        // Rétro-compat
        if (finalScoreText != null)
            finalScoreText.text = $"Score: {score:N0}";
        if (finalComboText != null && ScoreManager.Instance != null)
            finalComboText.text = $"Best Combo: {ScoreManager.Instance.BestCombo}x";
    }

    // ── Défaite ─────────────────────────────────────────

    private void ShowGameOverScreen(int score)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        if (gameOverScoreText != null)
            gameOverScoreText.text = $"Score\n{score:N0}";

        if (gameOverBestComboText != null && ScoreManager.Instance != null)
            gameOverBestComboText.text = $"Best Combo: {ScoreManager.Instance.BestCombo}x";

        // Rétro-compat
        if (finalScoreText != null)
            finalScoreText.text = $"Score: {score:N0}";
        if (finalComboText != null && ScoreManager.Instance != null)
            finalComboText.text = $"Best Combo: {ScoreManager.Instance.BestCombo}x";
    }

    // ── HUD ──────────────────────────────────────────────

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
            comboText.transform.localScale = (combo > 0 && combo % 10 == 0)
                ? Vector3.one * 1.5f
                : Vector3.one;
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

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
            ScoreManager.Instance.OnComboChanged -= UpdateCombo;
            ScoreManager.Instance.OnMultiplierChanged -= UpdateMultiplier;
        }

        if (SpeedMultiplier.Instance != null)
            SpeedMultiplier.Instance.OnSpeedChanged -= UpdateSpeed;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCountdownTick -= ShowCountdownTick;
            GameManager.Instance.OnVictory -= ShowVictoryScreen;
            GameManager.Instance.OnGameOver -= ShowGameOverScreen;
            GameManager.Instance.OnGameStarted -= HideCountdown;
        }
    }
}
