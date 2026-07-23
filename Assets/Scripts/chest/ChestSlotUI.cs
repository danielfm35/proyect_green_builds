using UnityEngine;
using UnityEngine.UI;

public class ChestSlotUI : MonoBehaviour
{
    private static readonly Color PrepareLinkedBorderColor = new Color(1f, 0.82f, 0.08f, 1f);

    private DraggedItemVisual currentItem;
    private RectTransform prepareLinkedBorder;
    private Image prepareLinkedBorderTop;
    private Image prepareLinkedBorderRight;
    private Image prepareLinkedBorderBottom;
    private Image prepareLinkedBorderLeft;

    public bool IsOccupied => currentItem != null;
    public DraggedItemVisual CurrentItem => currentItem;

    public bool CanAcceptItem()
    {
        return !IsOccupied;
    }

    public void SetItem(DraggedItemVisual item)
    {
        currentItem = item;
    }

    public void ClearItem()
    {
        currentItem = null;
    }

    public void SetPrepareLinkedHighlight(bool isHighlighted)
    {
        if (!isHighlighted && prepareLinkedBorder == null)
            return;

        EnsurePrepareLinkedBorder();
        if (prepareLinkedBorder != null)
            prepareLinkedBorder.gameObject.SetActive(isHighlighted);
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
}
