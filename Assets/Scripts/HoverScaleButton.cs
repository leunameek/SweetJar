using UnityEngine;
using UnityEngine.EventSystems;

public class HoverScaleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.08f;
    public float pressedScale = 1.02f;
    public float transitionSpeed = 12f;

    private RectTransform rectTransform;
    private Vector3 baseScale;
    private float targetScale = 1f;
    private bool isHovered;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        baseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
    }

    void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform != null)
        {
            baseScale = rectTransform.localScale;
            rectTransform.localScale = baseScale;
        }

        targetScale = 1f;
        isHovered = false;
    }

    void Update()
    {
        if (rectTransform == null)
            return;

        Vector3 desiredScale = baseScale * targetScale;
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, desiredScale, Time.unscaledDeltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = 1f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = isHovered ? hoverScale : 1f;
    }
}
