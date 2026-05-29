using UnityEngine;
using UnityEngine.EventSystems;
using FMODUnity;

public class UIButtonFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Hover Visual")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverOffsetX = -15f;
    [SerializeField] private float animationSpeed = 12f;

    [Header("FMOD Events")]
    [SerializeField] private EventReference hoverSound;
    [SerializeField] private EventReference clickSound;

    private Vector3 baseScale;
    private Vector3 targetScale;

    private Vector3 basePosition;
    private Vector3 targetPosition;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        baseScale = transform.localScale;
        targetScale = baseScale;

        basePosition = rectTransform.anchoredPosition;
        targetPosition = basePosition;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * animationSpeed
        );

        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            Time.deltaTime * animationSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = baseScale * hoverScale;

        targetPosition = basePosition + Vector3.right * hoverOffsetX;

        RuntimeManager.PlayOneShot(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = baseScale;
        targetPosition = basePosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RuntimeManager.PlayOneShot(clickSound);
    }
}