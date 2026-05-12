using UnityEngine;
using UnityEngine.UI;







public class EditorSlidingPanel : MonoBehaviour
{
    public enum SlideDirection { Left, Right }

    [Header("=== Configuration ===")]
    [Tooltip("Direction depuis laquelle le panneau entre")]
    public SlideDirection direction = SlideDirection.Right;

    [Tooltip("Largeur du panneau en pixels")]
    public float panelWidth = 350f;

    [Tooltip("Durée de l'animation de slide en secondes")]
    public float slideDuration = 0.3f;

    [Header("=== Références ===")]
    [Tooltip("Le RectTransform du panneau à animer")]
    public RectTransform panelRect;

    [Tooltip("Le bouton qui toggle l'ouverture/fermeture du panneau")]
    public Button toggleButton;

    [Tooltip("Texte ou icône du bouton (optionnel, change selon l'état)")]
    public TMPro.TextMeshProUGUI toggleButtonText;

    [Header("=== Icônes bouton (optionnel) ===")]
    public string openIcon = "☰";
    public string closeIcon = "✕";

    private bool isOpen = false;
    private float targetX;
    private float currentVelocity;

    
    public bool IsOpen => isOpen;

    private void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);

        
        SetPanelPosition(GetHiddenX());
        UpdateButtonText();
    }

    private void Update()
    {
        if (panelRect == null) return;

        float target = isOpen ? GetVisibleX() : GetHiddenX();
        Vector2 pos = panelRect.anchoredPosition;
        pos.x = Mathf.SmoothDamp(pos.x, target, ref currentVelocity, slideDuration);
        panelRect.anchoredPosition = pos;
    }

    
    
    
    public void Toggle()
    {
        isOpen = !isOpen;
        UpdateButtonText();
    }

    
    
    
    public void Open()
    {
        isOpen = true;
        UpdateButtonText();
    }

    
    
    
    public void Close()
    {
        isOpen = false;
        UpdateButtonText();
    }

    private float GetHiddenX()
    {
        
        
        return direction == SlideDirection.Right ? panelWidth : -panelWidth;
    }

    private float GetVisibleX()
    {
        return 0f;
    }

    private void SetPanelPosition(float x)
    {
        if (panelRect == null) return;
        Vector2 pos = panelRect.anchoredPosition;
        pos.x = x;
        panelRect.anchoredPosition = pos;
    }

    private void UpdateButtonText()
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.text = isOpen ? closeIcon : openIcon;
        }
    }
}
