using TMPro;
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

    [Header("Side Sections")]
    [SerializeField, Min(1)] private int enchantmentSlots = 4;
    [SerializeField, Min(1)] private int modifierSlots = 4;
    [SerializeField, Min(1)] private int sideColumns = 4;
    [SerializeField, Min(1f)] private float sideSlotSize = 60f;
    [SerializeField, Min(0f)] private float sideSpacing = 14f;

    [Header("Slot")]
    [SerializeField] private Sprite slotSprite;
    [SerializeField] private Color slotColor = Color.white;

    private GridLayoutGroup grid;
    private bool isRebuilding;
    private bool rebuildRequested;
    [SerializeField, HideInInspector] private bool dockedLayout;
    private Vector2 lastDockSize;
    private bool initialLayoutPending = true;
    private bool preparingLayout;

    private void OnEnable()
    {
        initialLayoutPending = true;
        Canvas.willRenderCanvases += PrepareLayoutForRendering;
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= PrepareLayoutForRendering;
    }

    private void PrepareLayoutForRendering()
    {
        if (!dockedLayout || preparingLayout || !isActiveAndEnabled ||
            !(transform.parent is RectTransform panel)) return;
        if (!initialLayoutPending && panel.rect.size == lastDockSize) return;

        preparingLayout = true;
        try
        {
            EnsureReferences();
            // Parent layout groups must finish before measuring the dock, including the first frame.
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.rootCanvas.transform as RectTransform);
            RefreshLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            initialLayoutPending = !HasUsableDockSize(panel);
        }
        finally
        {
            preparingLayout = false;
        }
    }

    private static bool HasUsableDockSize(RectTransform panel)
    {
        return panel.rect.width >= 160f && panel.rect.height >= 240f;
    }

    public void ConfigureDockedLayout()
    {
        // Reflow existing slots without destroying their contents or changing capacity.
        int capacity = Capacity;
        columns = capacity % 4 == 0 ? 4 : columns;
        rows = capacity / columns;
        dockedLayout = true;
        initialLayoutPending = true;
        PrepareLayoutForRendering();
    }

    private void LateUpdate()
    {
        PrepareLayoutForRendering();
    }

    public int Columns => columns;
    public int Rows => rows;
    public int Capacity => columns * rows;
    public int EnchantmentCapacity => enchantmentSlots;
    public int ModifierCapacity => modifierSlots;

    private void Awake()
    {
        EnsureReferences();
        RefreshLayout();
        rebuildRequested = NeedsRebuild();
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
        enchantmentSlots = Mathf.Max(1, enchantmentSlots);
        modifierSlots = Mathf.Max(1, modifierSlots);
        sideColumns = Mathf.Max(1, sideColumns);
        sideSlotSize = Mathf.Max(1f, sideSlotSize);
        sideSpacing = Mathf.Max(0f, sideSpacing);

        EnsureReferences();
        RefreshLayout();

        // OnValidate puede ejecutarse varias veces por una sola edición del
        // Inspector. Se agrupan todas en una reconstrucción en Update.
        rebuildRequested = NeedsRebuild();
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
                CreateSlot(transform, i, "InventorySlot", ChestSlotCategory.Item, slotSize);

            BuildSideSection(
                "EnchantmentInventorySection",
                "Encantamientos",
                enchantmentSlots,
                ChestSlotCategory.Enchantment,
                0.17f);

            BuildSideSection(
                "ModifierInventorySection",
                "Modificadores",
                modifierSlots,
                ChestSlotCategory.Modifier,
                0.83f);

            EnsureTitle("ItemInventoryTitle", "Items", 0.5f);

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

    private void CreateSlot(
        Transform parent,
        int index,
        string namePrefix,
        ChestSlotCategory category,
        float size)
    {
        GameObject slotObject = new GameObject(
            $"{namePrefix}_{index + 1:00}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(ChestSlotUI));

        slotObject.layer = gameObject.layer;
        slotObject.transform.SetParent(parent, false);

        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.sizeDelta = Vector2.one * size;

        Image image = slotObject.GetComponent<Image>();
        image.sprite = slotSprite;
        image.color = slotColor;
        image.raycastTarget = true;
        image.preserveAspect = false;

        slotObject.GetComponent<ChestSlotUI>().Configure(category);
    }

    private void BuildSideSection(
        string sectionName,
        string title,
        int capacity,
        ChestSlotCategory category,
        float anchorX)
    {
        Transform parent = transform.parent;
        if (parent == null)
            return;

        Transform existing = parent.Find(sectionName);
        if (existing != null)
            DestroyObject(existing.gameObject);

        GameObject sectionObject = new GameObject(sectionName, typeof(RectTransform), typeof(GridLayoutGroup));
        sectionObject.layer = gameObject.layer;
        sectionObject.transform.SetParent(parent, false);

        RectTransform sectionRect = sectionObject.GetComponent<RectTransform>();
        sectionRect.anchorMin = sectionRect.anchorMax = new Vector2(anchorX, 0.5f);
        sectionRect.pivot = new Vector2(0.5f, 0.5f);
        sectionRect.anchoredPosition = Vector2.zero;

        int usedColumns = Mathf.Min(sideColumns, capacity);
        int usedRows = Mathf.CeilToInt(capacity / (float)usedColumns);
        sectionRect.sizeDelta = new Vector2(
            usedColumns * sideSlotSize + (usedColumns - 1) * sideSpacing,
            usedRows * sideSlotSize + (usedRows - 1) * sideSpacing);

        GridLayoutGroup sideGrid = sectionObject.GetComponent<GridLayoutGroup>();
        sideGrid.cellSize = Vector2.one * sideSlotSize;
        sideGrid.spacing = Vector2.one * sideSpacing;
        sideGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        sideGrid.constraintCount = usedColumns;
        sideGrid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < capacity; i++)
            CreateSlot(sectionObject.transform, i, category + "Slot", category, sideSlotSize);

        EnsureTitle(sectionName + "Title", title, anchorX);
    }

    private void EnsureTitle(string objectName, string title, float anchorX)
    {
        Transform parent = transform.parent;
        if (parent == null)
            return;

        Transform existing = parent.Find(objectName);
        GameObject titleObject;
        if (existing == null)
        {
            titleObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObject.layer = gameObject.layer;
            titleObject.transform.SetParent(parent, false);
        }
        else
        {
            titleObject = existing.gameObject;
        }

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(anchorX, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 100f);
        titleRect.sizeDelta = new Vector2(320f, 34f);

        TextMeshProUGUI label = titleObject.GetComponent<TextMeshProUGUI>();
        label.text = title;
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private void RefreshLayout()
    {
        if (grid == null)
            return;

        if (dockedLayout && transform.parent is RectTransform panel)
        {
            // Keep the saved layout while Unity is still reporting an uninitialized parent rect.
            if (!HasUsableDockSize(panel))
            {
                initialLayoutPending = true;
                return;
            }
            lastDockSize = panel.rect.size;
            spacing = 10f;
            slotSize = Mathf.Max(1f, Mathf.Min(76f,
                (panel.rect.width - 48f - (columns - 1) * spacing) / columns,
                (panel.rect.height * 0.48f - 40f - (rows - 1) * spacing) / rows));
            PlaceDockSection(transform as RectTransform, 0.49f);
            PlaceDockTitle("ItemInventoryTitle", "Items", 0.49f,
                (rows * slotSize + (rows - 1) * spacing) * 0.5f + 24f);
            LayoutDockSide("EnchantmentInventorySection", "Encantamientos", 0.84f);
            LayoutDockSide("ModifierInventorySection", "Modificadores", 0.13f);
        }

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

    private void LayoutDockSide(string sectionName, string title, float anchorY)
    {
        RectTransform section = transform.parent.Find(sectionName) as RectTransform;
        if (section == null) return;
        GridLayoutGroup sideGrid = section.GetComponent<GridLayoutGroup>();
        if (sideGrid == null) return;
        float size = Mathf.Min(slotSize, 64f);
        int count = Mathf.Min(sideColumns, Mathf.Max(1, section.childCount));
        int sideRows = Mathf.CeilToInt(section.childCount / (float)count);
        sideGrid.cellSize = Vector2.one * size;
        sideGrid.spacing = Vector2.one * 10f;
        sideGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        sideGrid.constraintCount = count;
        section.sizeDelta = new Vector2(count * size + (count - 1) * 10f,
            sideRows * size + (sideRows - 1) * 10f);
        PlaceDockSection(section, anchorY);
        PlaceDockTitle(sectionName + "Title", title, anchorY, section.sizeDelta.y * 0.5f + 24f);
    }

    private static void PlaceDockSection(RectTransform rect, float anchorY)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, anchorY);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    private void PlaceDockTitle(string name, string title, float anchorY, float offset)
    {
        EnsureTitle(name, title, 0.5f);
        RectTransform rect = transform.parent.Find(name) as RectTransform;
        PlaceDockSection(rect, anchorY);
        rect.anchoredPosition = new Vector2(0f, offset);
        rect.sizeDelta = new Vector2(Mathf.Max(1f, lastDockSize.x - 32f), 30f);
        TextMeshProUGUI label = rect.GetComponent<TextMeshProUGUI>();
        label.fontSize = 19f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 19f;
        label.color = new Color(0.78f, 0.83f, 0.92f);
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

    private bool NeedsRebuild()
    {
        if (CountSlots() != Capacity || transform.parent == null)
            return true;

        Transform enchantmentSection = transform.parent.Find("EnchantmentInventorySection");
        Transform modifierSection = transform.parent.Find("ModifierInventorySection");
        return CountSlots(enchantmentSection) != enchantmentSlots
            || CountSlots(modifierSection) != modifierSlots;
    }

    private static int CountSlots(Transform root)
    {
        if (root == null)
            return 0;

        int count = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            if (root.GetChild(i).GetComponent<ChestSlotUI>() != null)
                count++;
        }

        return count;
    }

    private static void DestroyObject(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
        {
            target.transform.SetParent(null, false);
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private void EnsureReferences()
    {
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        if (slotSprite == null)
            slotSprite = Resources.Load<Sprite>("Images/UI/common");
    }
}
