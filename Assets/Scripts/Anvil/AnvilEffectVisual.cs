using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnvilEffectVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image targetRuleIndicatorImage;

    private AnvilEffectData effectData;
    private GridPlacementRule targetRule;
    private UIImpactScaleAnimation impactScaleAnimation;

    public AnvilEffectData EffectData => effectData;
    public GridPlacementRule TargetRule => targetRule;
    public float ImpactScaleAnimationDuration => EnsureImpactScaleAnimation().Duration;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (effectData == null)
            return;

        Canvas hoverCanvas = GetComponentInParent<Canvas>();
        AnvilEffectTooltipUI.Show(effectData, hoverCanvas);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnvilEffectTooltipUI.Hide();
    }

    private void OnDestroy()
    {
        AnvilEffectTooltipUI.Hide();
    }

    public void Initialize(AnvilEffectData data, GridPlacementRule newTargetRule = GridPlacementRule.None)
    {
        effectData = data;
        targetRule = newTargetRule;
        EnsureImages();

        if (backgroundImage != null)
        {
            backgroundImage.sprite = ResolveBackground(data);
            backgroundImage.color = data != null ? data.backgroundColor : Color.white;
            backgroundImage.preserveAspect = true;
            backgroundImage.raycastTarget = true;
        }

        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.icon : null;
            iconImage.enabled = data != null && data.icon != null;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        UpdateTargetRuleVisual(targetRule);
    }

    private static Sprite ResolveBackground(AnvilEffectData data)
    {
        if (data == null)
            return null;

        if (data.background != null)
            return data.background;

        string resourcePath = data.kind == AnvilEffectKind.Blessing
            ? "Images/UI/blessing"
            : "Images/UI/curse";
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        return sprites.Length > 0 ? sprites[0] : null;
    }
    public void PlayImpactScaleAnimation()
    {
        EnsureImpactScaleAnimation().Play();
    }

    private UIImpactScaleAnimation EnsureImpactScaleAnimation()
    {
        if (impactScaleAnimation == null)
            impactScaleAnimation = GetComponent<UIImpactScaleAnimation>();

        if (impactScaleAnimation == null)
            impactScaleAnimation = gameObject.AddComponent<UIImpactScaleAnimation>();

        return impactScaleAnimation;
    }

    private void UpdateTargetRuleVisual(GridPlacementRule rule)
    {
        EnsureTargetRuleIndicator();

        if (targetRuleIndicatorImage == null)
            return;

        if (rule == GridPlacementRule.None)
        {
            targetRuleIndicatorImage.enabled = false;
            return;
        }

        targetRuleIndicatorImage.sprite = GridPlacementRuleUtility.LoadRuleSprite(rule);
        targetRuleIndicatorImage.preserveAspect = true;
        targetRuleIndicatorImage.enabled = targetRuleIndicatorImage.sprite != null;
        targetRuleIndicatorImage.raycastTarget = false;
    }

    private void EnsureImages()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage == null)
            backgroundImage = gameObject.AddComponent<Image>();

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        if (iconImage != null)
            return;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(8f, 8f);
        iconRect.offsetMax = new Vector2(-8f, -8f);

        iconImage = iconObject.GetComponent<Image>();
    }

    private void EnsureTargetRuleIndicator()
    {
        if (targetRuleIndicatorImage != null)
            return;

        GameObject indicatorObject = new GameObject("TargetRuleIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        indicatorObject.transform.SetParent(transform, false);

        RectTransform indicatorRect = indicatorObject.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(1f, 0f);
        indicatorRect.anchorMax = new Vector2(1f, 0f);
        indicatorRect.pivot = new Vector2(1f, 0f);
        indicatorRect.anchoredPosition = new Vector2(-6f, 6f);
        indicatorRect.sizeDelta = new Vector2(24f, 24f);

        targetRuleIndicatorImage = indicatorObject.GetComponent<Image>();
        targetRuleIndicatorImage.enabled = false;
        targetRuleIndicatorImage.raycastTarget = false;
        targetRuleIndicatorImage.preserveAspect = true;
    }
}
