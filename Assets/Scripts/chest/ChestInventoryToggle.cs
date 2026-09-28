using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Keeps the serialized scene reference while replacing the old chest toggle with a permanent dock.
public sealed class ChestInventoryToggle : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform screenContainer;
    [SerializeField] private RectTransform inventoryPanel;
    [SerializeField, HideInInspector] private bool layoutApplied;

    public RectTransform EnchantmentTarget => inventoryPanel != null
        ? inventoryPanel.Find("EnchantmentInventorySection") as RectTransform : null;
    public RectTransform ModifierTarget => inventoryPanel != null
        ? inventoryPanel.Find("ModifierInventorySection") as RectTransform : null;

    private void Start()
    {
        if (!layoutApplied) ApplyPermanentLayout();
    }

    public bool ApplyPermanentLayout()
    {
        if (screenContainer == null || inventoryPanel == null) return false;

        // Empty layout backgrounds must not intercept the Start Battle button.
        if (screenContainer.TryGetComponent(out Image screenBackground)) screenBackground.raycastTarget = false;
        Transform top = screenContainer.Find("TopSection");
        if (top != null && top.TryGetComponent(out Image topBackground)) topBackground.raycastTarget = false;

        RectTransform anvil = screenContainer.Find("AnvilSection") as RectTransform;
        if (anvil == null) return false;
        Transform bottom = transform.parent;
        RectTransform actions = anvil.Find("ForgeActionButtons") as RectTransform;
        if (actions == null) actions = bottom.Find("ForgeActionButtons") as RectTransform;
        if (actions == null) return false;

        // The chest has no visible or interactive role in the permanent inventory.
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>()) graphic.enabled = false;
        UIHoverScale hover = GetComponent<UIHoverScale>();
        if (hover != null) hover.enabled = false;

        inventoryPanel.SetParent(anvil, false);
        inventoryPanel.SetAsLastSibling();
        Stretch(inventoryPanel, new Vector2(0.725f, 0f), Vector2.one, new Vector2(14f, 16f), new Vector2(-14f, -16f));
        LayoutElement layout = inventoryPanel.GetComponent<LayoutElement>();
        if (layout != null) layout.ignoreLayout = true;
        Image background = inventoryPanel.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.065f, 0.075f, 0.1f, 1f);
            background.raycastTarget = false;
        }
        ShowInventory();
        Canvas.ForceUpdateCanvases();
        ChestInventoryGridUI grid = inventoryPanel.GetComponentInChildren<ChestInventoryGridUI>(true);
        if (grid != null) grid.ConfigureDockedLayout();

        if (actions != null)
        {
            actions.SetParent(bottom, false);
            Stretch(actions, new Vector2(0.775f, 0f), Vector2.one, new Vector2(12f, 25f), new Vector2(-22f, -25f));
            StyleButton(actions.Find("EnchantmentsButton") as RectTransform, 0, "Images/UI/grimorio", new Color(0.72f, 0.51f, 1f));
            StyleButton(actions.Find("ModifiersButton") as RectTransform, 1, "Images/UI/tongs", new Color(0.3f, 0.78f, 0.94f));
            StyleButton(actions.Find("TransmuteButton") as RectTransform, 2, "Images/UI/anvil", new Color(1f, 0.72f, 0.32f));
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryPanel);
        Canvas.ForceUpdateCanvases();
        layoutApplied = true;
        return true;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Builds Battles/Aplicar y guardar inventario en Canvas")]
    private static void ApplyAndSaveEditorLayout()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Sal de Play para guardar la distribucion en la escena.");
            return;
        }
        ChestInventoryToggle dock = FindFirstObjectByType<ChestInventoryToggle>();
        if (dock == null || dock.screenContainer == null)
        {
            Debug.LogError("No se encontro el inventario en la escena abierta.");
            return;
        }
        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(dock.screenContainer.root.gameObject, "Distribuir inventario en Canvas");
        if (!dock.ApplyPermanentLayout())
        {
            Debug.LogError("Faltan referencias del Canvas para aplicar la distribucion.");
            return;
        }
        foreach (Transform child in dock.screenContainer.root.GetComponentsInChildren<Transform>(true))
        {
            UnityEditor.EditorUtility.SetDirty(child.gameObject);
            foreach (Component component in child.GetComponents<Component>())
                if (component != null) UnityEditor.EditorUtility.SetDirty(component);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dock.gameObject.scene);
        bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(dock.gameObject.scene);
        UnityEditor.Selection.activeGameObject = dock.inventoryPanel.gameObject;
        UnityEditor.SceneView.RepaintAll();
        if (saved) Debug.Log("Inventario y botones guardados en el Canvas, visibles sin entrar en Play.");
    }
#endif

    // Legacy callers cannot close the permanent inventory.
    public void OnPointerClick(PointerEventData eventData) { }
    public void HideInventory() => ShowInventory();
    public void ShowInventory()
    {
        if (inventoryPanel == null) return;
        inventoryPanel.gameObject.SetActive(true);
        CanvasGroup group = inventoryPanel.GetComponent<CanvasGroup>();
        if (group == null) group = inventoryPanel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private static void StyleButton(RectTransform rect, int index, string iconPath, Color accent)
    {
        if (rect == null) return;
        Stretch(rect, new Vector2(index / 3f, 0f), new Vector2((index + 1) / 3f, 1f), new Vector2(5f, 0f), new Vector2(-5f, 0f));
        Image face = rect.GetComponent<Image>();
        if (face != null) face.color = Color.white;
        Button button = rect.GetComponent<Button>();
        if (button != null)
        {
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.105f, 0.125f, 0.18f);
            colors.highlightedColor = Color.Lerp(colors.normalColor, accent, 0.32f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(colors.normalColor, accent, 0.5f);
            colors.disabledColor = new Color(0.12f, 0.12f, 0.14f, 0.5f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
        }
        Outline outline = rect.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = Color.Lerp(new Color(0.1f, 0.12f, 0.16f), accent, 0.6f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
        TextMeshProUGUI label = rect.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            Stretch(label.rectTransform, Vector2.zero, new Vector2(1f, 0.4f), new Vector2(4f, 8f), new Vector2(-4f, 0f));
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 16f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.95f, 0.96f, 1f);
            label.raycastTarget = false;
        }
        Transform existingIcon = rect.Find("ActionIcon");
        GameObject iconObject = existingIcon != null ? existingIcon.gameObject
            : new GameObject("ActionIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.layer = rect.gameObject.layer;
        iconObject.transform.SetParent(rect, false);
        RectTransform icon = iconObject.GetComponent<RectTransform>();
        Stretch(icon, new Vector2(0.18f, 0.4f), new Vector2(0.82f, 0.92f), Vector2.zero, Vector2.zero);
        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = Resources.Load<Sprite>(iconPath);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        Transform existingLine = rect.Find("Accent");
        GameObject lineObject = existingLine != null ? existingLine.gameObject
            : new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.layer = rect.gameObject.layer;
        lineObject.transform.SetParent(rect, false);
        Stretch(lineObject.GetComponent<RectTransform>(), new Vector2(0.15f, 1f), new Vector2(0.85f, 1f), new Vector2(0f, -3f), Vector2.zero);
        lineObject.GetComponent<Image>().color = accent;
        lineObject.GetComponent<Image>().raycastTarget = false;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 insetMin, Vector2 insetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = insetMin;
        rect.offsetMax = insetMax;
        rect.localScale = Vector3.one;
    }
}
