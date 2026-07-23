using UnityEngine;
using UnityEngine.EventSystems;

[ExecuteAlways]
public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.12f;
    [SerializeField] private float animationSpeed = 14f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        ApplyDefaultToolPosition();
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void OnEnable()
    {
        ApplyDefaultToolPosition();
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
    }

    private void ApplyDefaultToolPosition()
    {
        if (transform is not RectTransform rectTransform)
            return;

        if (gameObject.name == "HammerHorizontal")
            rectTransform.anchoredPosition = new Vector2(-90f, -70f);
        else if (gameObject.name == "TongsHorizontal")
            rectTransform.anchoredPosition = new Vector2(-90f, -270f);
    }
}
