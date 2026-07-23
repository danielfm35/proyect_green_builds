using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public sealed class ChestInventoryGridUI : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField, Min(1)] private int columns = 8;
    [SerializeField, Min(1)] private int rows = 2;
    [SerializeField, Min(1f)] private float slotSize = 71f;
    [SerializeField, Min(0f)] private float spacing = 12f;

    [Header("Slot")]
    [SerializeField] private Sprite slotSprite;
    [SerializeField] private Color slotColor = Color.white;

    private GridLayoutGroup grid;
    private bool isRebuilding;
    private bool rebuildRequested;

    public int Columns => columns;
    public int Rows => rows;
    public int Capacity => columns * rows;

    private void Awake()
    {
        EnsureReferences();
        RefreshLayout();
        rebuildRequested = CountSlots() != Capacity;
    }

    private void Update()
    {
        if (!rebuildRequested || isRebuilding)
            return;

        rebuildRequested = false;
        RebuildSlots();
    }

    private void OnValidate()
    {
        if (isRebuilding)
            return;

        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        slotSize = Mathf.Max(1f, slotSize);
        spacing = Mathf.Max(0f, spacing);

        EnsureReferences();
        RefreshLayout();

        // OnValidate puede ejecutarse varias veces por una sola edición del
        // Inspector. Se agrupan todas en una reconstrucción en Update.
        rebuildRequested = CountSlots() != Capacity;
    }

    public void SetGridSize(int newColumns, int newRows)
    {
        columns = Mathf.Max(1, newColumns);
        rows = Mathf.Max(1, newRows);
        RebuildSlots();
    }

    public void SetSlotLayout(float newSlotSize, float newSpacing)
    {
        slotSize = Mathf.Max(1f, newSlotSize);
        spacing = Mathf.Max(0f, newSpacing);
        RefreshLayout();
    }

    [ContextMenu("Rebuild Inventory Slots")]
    public void RebuildSlots()
    {
        if (isRebuilding)
            return;

        isRebuilding = true;
        EnsureReferences();

        try
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (child.GetComponent<ChestSlotUI>() == null)
                    continue;

                if (Application.isPlaying)
                {
                    // Destroy se completa al final del frame. Sacarlo primero
                    // evita que GridLayoutGroup mezcle slots viejos y nuevos.
                    child.transform.SetParent(null, false);
                    Destroy(child);
                }
                else
                    DestroyImmediate(child);
            }

            for (int i = 0; i < Capacity; i++)
                CreateSlot(i);

            RefreshLayout();

            ChestStorageManager storage = GetComponentInParent<ChestStorageManager>();
            if (storage != null)
                storage.RefreshSlots();
        }
        finally
        {
            isRebuilding = false;
        }
    }

    private void CreateSlot(int index)
    {
        GameObject slotObject = new GameObject(
            $"InventorySlot_{index + 1:00}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(ChestSlotUI));

        slotObject.layer = gameObject.layer;
        slotObject.transform.SetParent(transform, false);

        Image image = slotObject.GetComponent<Image>();
        image.sprite = slotSprite;
        image.color = slotColor;
        image.raycastTarget = true;
        image.preserveAspect = false;
    }

    private void RefreshLayout()
    {
        if (grid == null)
            return;

        grid.cellSize = Vector2.one * slotSize;
        grid.spacing = Vector2.one * spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = TextAnchor.MiddleCenter;

        RectTransform rect = transform as RectTransform;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(
                columns * slotSize + (columns - 1) * spacing,
                rows * slotSize + (rows - 1) * spacing);
        }
    }

    private int CountSlots()
    {
        int count = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).GetComponent<ChestSlotUI>() != null)
                count++;
        }

        return count;
    }

    private void EnsureReferences()
    {
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        if (slotSprite == null)
            slotSprite = Resources.Load<Sprite>("Images/UI/common");
    }
}
