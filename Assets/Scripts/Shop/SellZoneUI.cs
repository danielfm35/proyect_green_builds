using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SellZoneUI : MonoBehaviour
{
    private static SellZoneUI instance;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private TextMeshProUGUI sellText;
    private ShopManager shopManager;

    public static SellZoneUI GetOrCreate(Canvas targetCanvas)
    {
        if (instance != null)
            return instance;

        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (targetCanvas == null)
            return null;

        GameObject zoneObject = new GameObject("SellZone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(SellZoneUI));
        zoneObject.transform.SetParent(targetCanvas.transform, false);

        instance = zoneObject.GetComponent<SellZoneUI>();
        instance.Initialize(targetCanvas);
        return instance;
    }

    public bool ContainsScreenPoint(Vector2 screenPosition, Camera eventCamera)
    {
        if (rectTransform == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
    }

    public void Show(ItemInstance itemInstance)
    {
        int sellValue = GetSellValue(itemInstance);
        if (sellText != null)
            sellText.text = $"Vender por {sellValue} de oro";

        SetVisible(true);
        SetHighlighted(false);
    }

    public void UpdatePointer(Vector2 screenPosition, Camera eventCamera)
    {
        SetHighlighted(ContainsScreenPoint(screenPosition, eventCamera));
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void Sell(DraggedItemVisual itemVisual)
    {
        if (itemVisual == null || itemVisual.ItemInstance == null)
            return;

        if (shopManager == null)
            shopManager = FindFirstObjectByType<ShopManager>();

        if (shopManager != null)
            shopManager.AddGold(GetSellValue(itemVisual.ItemInstance));

        PrepareSlotUI.ClearDestroyedItem(itemVisual);
        Destroy(itemVisual.gameObject);
        Hide();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Initialize(GetComponentInParent<Canvas>());
    }

    private void Initialize(Canvas targetCanvas)
    {
        canvas = targetCanvas;
        rectTransform = GetComponent<RectTransform>();
        backgroundImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        shopManager = FindFirstObjectByType<ShopManager>();

        ConfigureRoot();
        EnsureText();
        SetVisible(false);
    }

    private void ConfigureRoot()
    {
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, 200f);
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0.02f, 0.025f, 0.025f, 0.86f);
            backgroundImage.raycastTarget = false;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private void EnsureText()
    {
        if (sellText != null)
            return;

        GameObject textObject = new GameObject("SellText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        sellText = textObject.GetComponent<TextMeshProUGUI>();
        sellText.raycastTarget = false;
        sellText.fontSize = 34f;
        sellText.fontStyle = FontStyles.Bold;
        sellText.alignment = TextAlignmentOptions.Center;
        sellText.color = new Color(1f, 0.92f, 0.55f, 1f);
        sellText.enableAutoSizing = true;
        sellText.fontSizeMin = 20f;
        sellText.fontSizeMax = 34f;
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
    }

    private void SetHighlighted(bool highlighted)
    {
        if (backgroundImage == null)
            return;

        backgroundImage.color = highlighted
            ? new Color(0.11f, 0.16f, 0.12f, 0.94f)
            : new Color(0.02f, 0.025f, 0.025f, 0.86f);
    }

    private static int GetSellValue(ItemInstance itemInstance)
    {
        if (itemInstance == null)
            return 0;

        return ShopManager.GetGoldCost(itemInstance.rarity);
    }
}
