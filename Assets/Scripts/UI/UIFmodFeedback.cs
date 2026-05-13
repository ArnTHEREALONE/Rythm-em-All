using UnityEngine;
using UnityEngine.EventSystems;
using FMODUnity;

public class UIFmodFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("=== SFX (FMOD) ===")]
    [Tooltip("Son joué au survol de la souris")]
    public EventReference hoverSFX;

    [Tooltip("Son joué au clic")]
    public EventReference clickSFX;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hoverSFX.IsNull)
        {
            RuntimeManager.PlayOneShot(hoverSFX);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!clickSFX.IsNull)
        {
            RuntimeManager.PlayOneShot(clickSFX);
        }
    }
}
