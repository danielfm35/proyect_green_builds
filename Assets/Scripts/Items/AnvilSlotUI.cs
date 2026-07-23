using UnityEngine;
using UnityEngine.UI;

public class AnvilSlotUI : MonoBehaviour
{
    private static readonly Color ValidPlacementHighlightColor = new Color(0.2f, 0.85f, 0.35f, 0.55f);
    private static readonly Color InvalidPlacementHighlightColor = new Color(0.9f, 0.15f, 0.15f, 0.55f);
    private static readonly Color BlessingTargetBorderColor = new Color(0.1f, 1f, 0.35f, 1f);
    private static readonly Color CurseTargetBorderColor = new Color(1f, 0.12f, 0.12f, 1f);
    private static readonly Color PrepareLinkedBorderColor = new Color(1f, 0.82f, 0.08f, 1f);

    [SerializeField] private int row;
    [SerializeField] private int column;
    [SerializeField] private Image backgroundImage;

    private DraggedItemVisual currentItem;
    private AnvilEffectVisual currentEffect;
    private RectTransform effectTargetBorder;
    private Image effectTargetBorderTop;
    private Image effectTargetBorderRight;
    private Image effectTargetBorderBottom;
    private Image effectTargetBorderLeft;
    private RectTransform prepareLinkedBorder;
    private Image prepareLinkedBorderTop;
    private Image prepareLinkedBorderRight;
    private Image prepareLinkedBorderBottom;
    private Image prepareLinkedBorderLeft;
    private RectTransform placementPreviewBorder;
    private Image placementPreviewBorderTop;
    private Image placementPreviewBorderRight;
    private Image placementPreviewBorderBottom;
    private Image placementPreviewBorderLeft;

    public int Row => row;
    public int Column => column;

    public bool IsOccupied => currentItem != null || currentEffect != null;
    public bool HasItem => currentItem != null;
    public bool HasEffect => currentEffect != null;
    public DraggedItemVisual CurrentItem => currentItem;
    public AnvilEffectVisual CurrentEffect => currentEffect;

    private void Awake()
    {
        EnsureBackgroundImage();
    }

    public bool CanAcceptItem()
    {
        return !IsOccupied;
    }

    public void SetCoordinates(int newRow, int newColumn)
    {
        row = newRow;
        column = newColumn;
    }

    public void SetItem(DraggedItemVisual item)
    {
        ClearEffect();
        currentItem = item;
    }

    public void ClearItem()
    {
        currentItem = null;
    }

    public void SetEffect(AnvilEffectVisual effect)
    {
        ClearItem();
        currentEffect = effect;
    }

    public void ClearEffect()
    {
        if (currentEffect == null)
            return;

        AnvilEffectVisual effectToDestroy = currentEffect;
        currentEffect = null;

        if (effectToDestroy != null)
            Destroy(effectToDestroy.gameObject);
    }

    public void ShowPlacementPreview(bool canPlace)
    {
        EnsurePlacementPreviewBorder();
        SetPlacementPreviewBorderColor(canPlace ? ValidPlacementHighlightColor : InvalidPlacementHighlightColor);
        if (placementPreviewBorder != null)
            placementPreviewBorder.gameObject.SetActive(true);
    }

    public void ClearPlacementPreview()
    {
        if (placementPreviewBorder != null)
            placementPreviewBorder.gameObject.SetActive(false);
    }

    public void ShowEffectTargetHighlight(AnvilEffectKind effectKind)
    {
        EnsureEffectTargetBorder();
        if (effectTargetBorder == null)
            return;

        Color borderColor = effectKind == AnvilEffectKind.Blessing
            ? BlessingTargetBorderColor
            : CurseTargetBorderColor;

        SetEffectTargetBorderColor(borderColor);
        effectTargetBorder.gameObject.SetActive(true);
    }

    public void ClearEffectTargetHighlight()
    {
        if (effectTargetBorder != null)
            effectTargetBorder.gameObject.SetActive(false);
    }

    public void SetPrepareLinkedHighlight(bool isHighlighted)
    {
        if (!isHighlighted && prepareLinkedBorder == null)
            return;

        EnsurePrepareLinkedBorder();
        if (prepareLinkedBorder != null)
            prepareLinkedBorder.gameObject.SetActive(isHighlighted);
    }

    private void EnsureBackgroundImage()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }

    private void EnsureEffectTargetBorder()
    {
        if (effectTargetBorder != null)
            return;

        GameObject borderObject = new GameObject("EffectTargetBorder", typeof(RectTransform));
        borderObject.transform.SetParent(transform, false);
        borderObject.transform.SetAsLastSibling();

        effectTargetBorder = borderObject.GetComponent<RectTransform>();
        effectTargetBorder.anchorMin = Vector2.zero;
        effectTargetBorder.anchorMax = Vector2.one;
        effectTargetBorder.offsetMin = Vector2.zero;
        effectTargetBorder.offsetMax = Vector2.zero;

        effectTargetBorderTop = CreateBorderLine("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 3f));
        effectTargetBorderRight = CreateBorderLine("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(3f, 0f));
        effectTargetBorderBottom = CreateBorderLine("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 3f));
        effectTargetBorderLeft = CreateBorderLine("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f));

        effectTargetBorder.gameObject.SetActive(false);
    }

    private Image CreateBorderLine(string lineName, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject lineObject = new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(effectTargetBorder, false);

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = anchorMin;
        lineRect.anchorMax = anchorMax;
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = Vector2.zero;
        lineRect.sizeDelta = sizeDelta;

        Image lineImage = lineObject.GetComponent<Image>();
        lineImage.raycastTarget = false;
        return lineImage;
    }

    private void SetEffectTargetBorderColor(Color color)
    {
        if (effectTargetBorderTop != null) effectTargetBorderTop.color = color;
        if (effectTargetBorderRight != null) effectTargetBorderRight.color = color;
        if (effectTargetBorderBottom != null) effectTargetBorderBottom.color = color;
        if (effectTargetBorderLeft != null) effectTargetBorderLeft.color = color;
    }

    private void EnsurePrepareLinkedBorder()
    {
        if (prepareLinkedBorder != null)
            return;

        GameObject borderObject = new GameObject("PrepareLinkedBorder", typeof(RectTransform));
        borderObject.transform.SetParent(transform, false);
        borderObject.transform.SetAsLastSibling();

        prepareLinkedBorder = borderObject.GetComponent<RectTransform>();
        prepareLinkedBorder.anchorMin = Vector2.zero;
        prepareLinkedBorder.anchorMax = Vector2.one;
        prepareLinkedBorder.offsetMin = Vector2.zero;
        prepareLinkedBorder.offsetMax = Vector2.zero;

        prepareLinkedBorderTop = CreatePrepareLinkedBorderLine("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 5f));
        prepareLinkedBorderRight = CreatePrepareLinkedBorderLine("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(5f, 0f));
        prepareLinkedBorderBottom = CreatePrepareLinkedBorderLine("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 5f));
        prepareLinkedBorderLeft = CreatePrepareLinkedBorderLine("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(5f, 0f));

        SetPrepareLinkedBorderColor(PrepareLinkedBorderColor);
        prepareLinkedBorder.gameObject.SetActive(false);
    }

    private Image CreatePrepareLinkedBorderLine(string lineName, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject lineObject = new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(prepareLinkedBorder, false);

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = anchorMin;
        lineRect.anchorMax = anchorMax;
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = Vector2.zero;
        lineRect.sizeDelta = sizeDelta;

        Image lineImage = lineObject.GetComponent<Image>();
        lineImage.raycastTarget = false;
        return lineImage;
    }

    private void SetPrepareLinkedBorderColor(Color color)
    {
        if (prepareLinkedBorderTop != null) prepareLinkedBorderTop.color = color;
        if (prepareLinkedBorderRight != null) prepareLinkedBorderRight.color = color;
        if (prepareLinkedBorderBottom != null) prepareLinkedBorderBottom.color = color;
        if (prepareLinkedBorderLeft != null) prepareLinkedBorderLeft.color = color;
    }

    private void EnsurePlacementPreviewBorder()
    {
        if (placementPreviewBorder != null)
            return;

        GameObject borderObject = new GameObject("PlacementPreviewBorder", typeof(RectTransform));
        borderObject.transform.SetParent(transform, false);
        borderObject.transform.SetAsLastSibling();

        placementPreviewBorder = borderObject.GetComponent<RectTransform>();
        placementPreviewBorder.anchorMin = Vector2.zero;
        placementPreviewBorder.anchorMax = Vector2.one;
        placementPreviewBorder.offsetMin = Vector2.zero;
        placementPreviewBorder.offsetMax = Vector2.zero;

        placementPreviewBorderTop = CreatePlacementPreviewBorderLine("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 5f));
        placementPreviewBorderRight = CreatePlacementPreviewBorderLine("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(5f, 0f));
        placementPreviewBorderBottom = CreatePlacementPreviewBorderLine("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 5f));
        placementPreviewBorderLeft = CreatePlacementPreviewBorderLine("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(5f, 0f));

        placementPreviewBorder.gameObject.SetActive(false);
    }

    private Image CreatePlacementPreviewBorderLine(string lineName, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject lineObject = new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(placementPreviewBorder, false);

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = anchorMin;
        lineRect.anchorMax = anchorMax;
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = Vector2.zero;
        lineRect.sizeDelta = sizeDelta;

        Image lineImage = lineObject.GetComponent<Image>();
        lineImage.raycastTarget = false;
        return lineImage;
    }

    private void SetPlacementPreviewBorderColor(Color color)
    {
        if (placementPreviewBorderTop != null) placementPreviewBorderTop.color = color;
        if (placementPreviewBorderRight != null) placementPreviewBorderRight.color = color;
        if (placementPreviewBorderBottom != null) placementPreviewBorderBottom.color = color;
        if (placementPreviewBorderLeft != null) placementPreviewBorderLeft.color = color;
    }
}
