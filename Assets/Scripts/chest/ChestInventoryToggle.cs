using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ChestInventoryToggle : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform screenContainer;
    [SerializeField] private RectTransform topSection;
    [SerializeField] private RectTransform secondarySection;
    [SerializeField] private RectTransform inventoryPanel;
    [SerializeField, Min(0.05f)] private float transitionDuration = 0.3f;
    [SerializeField] private float slideDistance = 90f;

    private readonly List<RectTransform> originalSections = new();
    private readonly List<Vector2> originalPositions = new();
    private readonly List<CanvasGroup> originalGroups = new();

    private CanvasGroup inventoryGroup;
    private Coroutine transition;
    private bool inventoryVisible;

    private IEnumerator Start()
    {
        // Wait until the parent VerticalLayoutGroup has placed both sections.
        yield return null;
        CacheOriginalSections();
        InitializeInventoryPanel();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (transition != null)
            StopCoroutine(transition);

        inventoryVisible = !inventoryVisible;
        transition = StartCoroutine(AnimateView(inventoryVisible));
    }

    private void CacheOriginalSections()
    {
        originalSections.Clear();
        originalPositions.Clear();
        originalGroups.Clear();

        AddOriginalSection(topSection);
        AddOriginalSection(secondarySection);
    }

    private void AddOriginalSection(RectTransform section)
    {
        if (section == null)
            return;

        CanvasGroup group = section.GetComponent<CanvasGroup>();
        if (group == null)
            group = section.gameObject.AddComponent<CanvasGroup>();

        originalSections.Add(section);
        originalPositions.Add(section.anchoredPosition);
        originalGroups.Add(group);
    }

    private void InitializeInventoryPanel()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[ChestInventoryToggle] Falta asignar ChestInventorySection en Inventory Panel.", this);
            return;
        }

        inventoryPanel.anchoredPosition = new Vector2(slideDistance, 0f);

        RectTransform inventoryGrid = inventoryPanel.Find("InventoryGrid") as RectTransform;
        if (inventoryGrid != null)
        {
            inventoryGrid.anchorMin = new Vector2(0.5f, 0.5f);
            inventoryGrid.anchorMax = new Vector2(0.5f, 0.5f);
            inventoryGrid.pivot = new Vector2(0.5f, 0.5f);
            inventoryGrid.anchoredPosition = Vector2.zero;
        }

        inventoryGroup = inventoryPanel.GetComponent<CanvasGroup>();
        if (inventoryGroup == null)
        {
            Debug.LogError("[ChestInventoryToggle] ChestInventorySection necesita un CanvasGroup.", inventoryPanel);
            return;
        }

        inventoryGroup.alpha = 0f;
        inventoryGroup.interactable = false;
        inventoryGroup.blocksRaycasts = false;
    }

    private IEnumerator AnimateView(bool showInventory)
    {
        if (inventoryPanel == null)
            yield break;

        float elapsed = 0f;
        float inventoryStartAlpha = inventoryGroup.alpha;
        float inventoryEndAlpha = showInventory ? 1f : 0f;
        Vector2 inventoryStartPosition = inventoryPanel.anchoredPosition;
        Vector2 inventoryEndPosition = showInventory ? Vector2.zero : new Vector2(slideDistance, 0f);

        float[] sectionStartAlpha = new float[originalGroups.Count];
        Vector2[] sectionStartPosition = new Vector2[originalSections.Count];
        for (int i = 0; i < originalGroups.Count; i++)
        {
            sectionStartAlpha[i] = originalGroups[i].alpha;
            sectionStartPosition[i] = originalSections[i].anchoredPosition;
            originalGroups[i].interactable = false;
            originalGroups[i].blocksRaycasts = false;
        }

        inventoryPanel.gameObject.SetActive(true);
        inventoryGroup.interactable = false;
        inventoryGroup.blocksRaycasts = false;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

            inventoryGroup.alpha = Mathf.Lerp(inventoryStartAlpha, inventoryEndAlpha, t);
            inventoryPanel.anchoredPosition = Vector2.LerpUnclamped(inventoryStartPosition, inventoryEndPosition, t);

            for (int i = 0; i < originalGroups.Count; i++)
            {
                float targetAlpha = showInventory ? 0f : 1f;
                Vector2 targetPosition = originalPositions[i] + (showInventory ? Vector2.left * slideDistance : Vector2.zero);
                originalGroups[i].alpha = Mathf.Lerp(sectionStartAlpha[i], targetAlpha, t);
                originalSections[i].anchoredPosition = Vector2.LerpUnclamped(sectionStartPosition[i], targetPosition, t);
            }

            yield return null;
        }

        inventoryGroup.alpha = inventoryEndAlpha;
        inventoryPanel.anchoredPosition = inventoryEndPosition;
        inventoryGroup.interactable = showInventory;
        inventoryGroup.blocksRaycasts = showInventory;

        for (int i = 0; i < originalGroups.Count; i++)
        {
            originalGroups[i].alpha = showInventory ? 0f : 1f;
            originalSections[i].anchoredPosition = originalPositions[i] + (showInventory ? Vector2.left * slideDistance : Vector2.zero);
            originalGroups[i].interactable = !showInventory;
            originalGroups[i].blocksRaycasts = !showInventory;
        }

        transition = null;
    }
}
