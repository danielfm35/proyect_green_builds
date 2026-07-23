using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int itemsPerRefresh = 5;
    [SerializeField, Range(0f, 1f)] private float chanceWithoutPlacementRule = 0.3f;
    [SerializeField, Min(0)] private int startingGold = 20;
    [SerializeField, Min(0)] private int refreshCost = 1;

    [Header("Gold UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button refreshButton;

    [Header("UI Slots")]
    [SerializeField] private List<ShopSlotUI> shopSlots = new();

    private ItemData[] availableItems;
    private readonly List<ItemInstance> currentShopItems = new();
    private int currentGold;
    private Sprite goldSprite;

    public int CurrentGold => currentGold;

    private void Start()
    {
        currentGold = startingGold;
        EnsureRefreshPriceDisplay();
        UpdateGoldUI();

        availableItems = Resources.LoadAll<ItemData>("ScriptableObjects/Items")
            .Where(item => item != null && !item.isCraftedOnly)
            .ToArray();
        Debug.Log($"Items cargados: {availableItems.Length}");
        RollShopItems();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        currentGold += amount;
        UpdateGoldUI();
    }

    public bool CanSpendGold(int amount)
    {
        return amount >= 0 && currentGold >= amount;
    }

    public bool TrySpendGold(int amount)
    {
        if (!CanSpendGold(amount))
            return false;

        currentGold -= amount;
        UpdateGoldUI();
        return true;
    }

    public void SetGold(int amount)
    {
        currentGold = Mathf.Max(0, amount);
        UpdateGoldUI();
    }

    public static int GetGoldCost(ItemRarity rarity)
    {
        return Mathf.Clamp((int)rarity + 1, 1, 5);
    }

    public void RefreshShop()
    {
        if (!TrySpendGold(refreshCost))
        {
            Debug.LogWarning($"No hay oro suficiente para refrescar la tienda. Costo={refreshCost}, oro={currentGold}");
            return;
        }

        RollShopItems();
    }

    private void RollShopItems()
    {
        currentShopItems.Clear();

        if (availableItems == null || availableItems.Length == 0)
        {
            Debug.LogWarning("No se encontraron ItemData disponibles para la tienda en Resources/ScriptableObjects/Items");
            ClearAllSlots();
            return;
        }

        for (int i = 0; i < itemsPerRefresh; i++)
        {
            int randomIndex = Random.Range(0, availableItems.Length);
            ItemData selectedItem = availableItems[randomIndex];

            ItemInstance instance = new ItemInstance(
                selectedItem,
                GetRandomSide(),
                GetRandomPlacementRule()
            );

            currentShopItems.Add(instance);
        }

        UpdateShopUI();
    }

    public static CrafterSide GetRandomSide()
    {
        return (CrafterSide)Random.Range(0, 4);
    }

    private GridPlacementRule GetRandomPlacementRule()
    {
        if (Random.value < chanceWithoutPlacementRule)
            return GridPlacementRule.None;

        IReadOnlyList<GridPlacementRule> selectableRules = GridPlacementRuleUtility.GetSelectableRules();
        if (selectableRules.Count == 0)
            return GridPlacementRule.None;

        return selectableRules[Random.Range(0, selectableRules.Count)];
    }

    private void UpdateShopUI()
    {
        for (int i = 0; i < shopSlots.Count; i++)
        {
            if (i < currentShopItems.Count)
            {
                Debug.Log(
                    $"Slot {i}: {currentShopItems[i].data.itemName} - side={currentShopItems[i].crafterSide}, " +
                    $"placement={currentShopItems[i].placementRule}"
                );
                shopSlots[i].SetItem(currentShopItems[i]);
            }
            else
            {
                shopSlots[i].ClearSlot();
            }
        }
    }

    private void ClearAllSlots()
    {
        for (int i = 0; i < shopSlots.Count; i++)
        {
            shopSlots[i].ClearSlot();
        }
    }

    private void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = currentGold.ToString();

        if (refreshButton != null)
            refreshButton.interactable = CanSpendGold(refreshCost);
    }

    private void EnsureRefreshPriceDisplay()
    {
        if (refreshButton == null)
        {
            GameObject refreshButtonObject = GameObject.Find("RefreshButton");
            if (refreshButtonObject != null)
                refreshButton = refreshButtonObject.GetComponent<Button>();
        }

        if (refreshButton == null || refreshButton.transform.Find("RefreshCostIcon") != null)
            return;

        if (goldSprite == null)
            goldSprite = Resources.Load<Sprite>("Images/UI/gold");

        GameObject iconObject = new GameObject("RefreshCostIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(refreshButton.transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0f);
        iconRect.anchorMax = new Vector2(0.5f, 0f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(-12f, -8f);
        iconRect.sizeDelta = new Vector2(22f, 22f);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = goldSprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        GameObject textObject = new GameObject("RefreshCostText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(refreshButton.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 0f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(2f, -11f);
        textRect.sizeDelta = new Vector2(28f, 24f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = refreshCost.ToString();
        text.raycastTarget = false;
        text.fontSize = 20f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Left;
        text.color = new Color(1f, 0.84f, 0.21f, 1f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 20f;
    }
}
