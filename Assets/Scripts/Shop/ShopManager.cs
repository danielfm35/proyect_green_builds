using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
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
    private Image refreshCostIcon;
    private TextMeshProUGUI refreshCostText;
    private bool isStartingBattle;
    private bool hasAppliedFirstAttackHealth;
    private RectTransform abilityCombatLayer;
    private readonly List<ActiveAbilityEffect> activeAbilityEffects = new();

    private sealed class PendingAbility
    {
        public AbilityData Data;
        public RectTransform Visual;
    }

    private sealed class ActiveAbilityEffect
    {
        public AbilityData Data;
        public int RemainingEnemyAttacks;
        public RectTransform Visual;
    }

    private readonly struct PendingMultiplier
    {
        public readonly float Value;
        public readonly EnchantmentData Enchantment;

        public PendingMultiplier(float value, EnchantmentData enchantment)
        {
            Value = value;
            Enchantment = enchantment;
        }
    }

    public int CurrentGold => currentGold;
    public void PrepareNextEncounter()
    {
        foreach (var effect in activeAbilityEffects)
            if (effect.Visual != null) Destroy(effect.Visual.gameObject);
        activeAbilityEffects.Clear();
    }
    public RectTransform GoldTarget => goldText != null ? goldText.rectTransform : null;

    private void OnEnable()
    {
        EnsureStartBattleButton();

#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        currentGold = startingGold;
        EnsureRefreshPriceDisplay();
        EnsureStartBattleButton();
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
        return 1;
    }

    public int GetCurrentItemCost(ItemRarity rarity)
    {
        return 1;
    }

    public void RefreshShop()
    {
        if (!TrySpendGold(refreshCost))
        {
            Debug.LogWarning($"No hay oro suficiente para refrescar la tienda. Costo={refreshCost}, oro={currentGold}");
            return;
        }

        RollShopItems();
        UpdateGoldUI();
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

        if (refreshCostIcon != null)
            refreshCostIcon.enabled = refreshCostIcon.sprite != null;

        if (refreshCostText != null)
        {
            refreshCostText.text = refreshCost.ToString();
            refreshCostText.color = new Color(1f, 0.84f, 0.21f, 1f);
        }

        foreach (ShopSlotUI slot in shopSlots)
        {
            if (slot != null)
                slot.RefreshPriceVisual();
        }
    }

    private void EnsureRefreshPriceDisplay()
    {
        if (refreshButton == null)
        {
            GameObject refreshButtonObject = GameObject.Find("RefreshButton");
            if (refreshButtonObject != null)
                refreshButton = refreshButtonObject.GetComponent<Button>();
        }

        if (refreshButton == null)
            return;

        if (goldSprite == null)
            goldSprite = Resources.Load<Sprite>("Images/UI/gold");

        Transform existingIcon = refreshButton.transform.Find("RefreshCostIcon");
        GameObject iconObject = existingIcon != null
            ? existingIcon.gameObject
            : new GameObject("RefreshCostIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existingIcon == null)
            iconObject.transform.SetParent(refreshButton.transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0f);
        iconRect.anchorMax = new Vector2(0.5f, 0f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(-12f, -8f);
        iconRect.sizeDelta = new Vector2(22f, 22f);

        refreshCostIcon = iconObject.GetComponent<Image>();
        refreshCostIcon.sprite = goldSprite;
        refreshCostIcon.preserveAspect = true;
        refreshCostIcon.raycastTarget = false;

        Transform existingText = refreshButton.transform.Find("RefreshCostText");
        GameObject textObject = existingText != null
            ? existingText.gameObject
            : new GameObject("RefreshCostText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (existingText == null)
            textObject.transform.SetParent(refreshButton.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 0f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(2f, -11f);
        textRect.sizeDelta = new Vector2(28f, 24f);

        refreshCostText = textObject.GetComponent<TextMeshProUGUI>();
        refreshCostText.raycastTarget = false;
        refreshCostText.fontSize = 20f;
        refreshCostText.fontStyle = FontStyles.Bold;
        refreshCostText.alignment = TextAlignmentOptions.Left;
        refreshCostText.enableAutoSizing = true;
        refreshCostText.fontSizeMin = 14f;
        refreshCostText.fontSizeMax = 20f;
    }

    private void EnsureStartBattleButton()
    {
        Canvas canvas = goldText != null ? goldText.canvas?.rootCanvas : null;
        if (canvas == null && refreshButton != null)
            canvas = refreshButton.GetComponentInParent<Canvas>()?.rootCanvas;
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>()?.rootCanvas;
        if (canvas == null)
            return;

        Transform existingButton = canvas.transform.Find("StartBattleButton");
        GameObject buttonObject;

        if (existingButton != null)
        {
            buttonObject = existingButton.gameObject;
        }
        else
        {
            buttonObject = new GameObject(
                "StartBattleButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );
            buttonObject.transform.SetParent(canvas.transform, false);
        }

        // Keep exactly one battle button, always under the main UI canvas.
        // A nested canvas can otherwise be returned first during Play Mode and
        // create a duplicate outside the chest's sorting hierarchy.
        RectTransform[] allRects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (RectTransform candidate in allRects)
        {
            if (candidate == null || candidate.gameObject == buttonObject || candidate.name != "StartBattleButton")
                continue;

            if (Application.isPlaying)
                Destroy(candidate.gameObject);
            else
                DestroyImmediate(candidate.gameObject);
        }

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-150f, -115f);
        buttonRect.sizeDelta = new Vector2(210f, 64f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.45f, 0.85f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.RemoveListener(StartBattle);
        button.onClick.AddListener(StartBattle);

        Transform existingLabel = buttonObject.transform.Find("Label");
        GameObject labelObject = existingLabel != null
            ? existingLabel.gameObject
            : new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (existingLabel == null)
            labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Start Battle";
        label.fontSize = 24f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private void StartBattle()
    {
        BossUI opponent = FindFirstObjectByType<BossUI>();
        if (opponent == null || opponent.CurrentHealth <= 0) return;
        if (!isStartingBattle)
            StartCoroutine(ResolveItemStatsAndStartBattle());
    }

    private IEnumerator ResolveItemStatsAndStartBattle()
    {
        isStartingBattle = true;

        Button startButton = GameObject.Find("StartBattleButton")?.GetComponent<Button>();
        if (startButton != null)
            startButton.interactable = false;

        PlayerUI playerUI = FindFirstObjectByType<PlayerUI>();
        BossUI bossUI = FindFirstObjectByType<BossUI>();
        PlayerData playerData = playerUI != null ? playerUI.Data : null;
        float healthMultiplier = playerData != null ? playerData.HealthMultiplier : 1f;
        float shieldMultiplier = playerData != null ? playerData.ShieldMultiplier : 1f;
        float damageMultiplier = playerData != null ? playerData.DamageMultiplier : 1f;
        float magicResistMultiplier = playerData != null ? playerData.MagicResistMultiplier : 1f;
        float healthPoints = 0f;
        float shieldPoints = 0f;
        float damagePoints = 0f;
        float magicResistPoints = 0f;
        if (playerUI != null)
            playerUI.SetCombatDefenses(0f, 0f);

        AnvilSlotUI[] slots = FindObjectsByType<AnvilSlotUI>(FindObjectsSortMode.None)
            .OrderBy(slot => slot.Row)
            .ThenBy(slot => slot.Column)
            .ToArray();

        int rowCount = slots.Length > 0 ? slots.Max(slot => slot.Row) + 1 : 0;
        int columnCount = slots.Length > 0 ? slots.Max(slot => slot.Column) + 1 : 0;
        Dictionary<AnvilSlotUI, List<PendingMultiplier>> multipliersBySlot =
            CollectMultipliersByTarget(slots, rowCount, columnCount);

        foreach (AnvilSlotUI slot in slots)
        {
            DraggedItemVisual itemVisual = slot != null ? slot.CurrentItem : null;
            ItemData itemData = itemVisual != null ? itemVisual.ItemData : null;
            if (itemData == null)
                continue;

            if (itemData.stats != null)
            {
                foreach (ItemStat stat in itemVisual.ItemInstance.GetEffectiveStats())
                {
                    if (stat == null || Mathf.Approximately(stat.value, 0f))
                        continue;

                    StatTarget target = GetStatTarget(stat.statType);
                    if (target == StatTarget.None)
                        continue;

                    TMP_Text destination = playerUI == null ? null : target switch
                    {
                        StatTarget.Health => playerUI.HealthStatText,
                        StatTarget.Shield => playerUI.ShieldStatText,
                        StatTarget.MagicResist => playerUI.MagicResistStatText,
                        _ => playerUI.DamageStatText
                    };
                    yield return StatTransferEffect.Play(
                        itemVisual.transform as RectTransform, destination, stat.value,
                        GetStatColor(target), delta =>
                    {
                        switch (target)
                        {
                            case StatTarget.Health:
                                healthPoints += delta;
                                break;
                            case StatTarget.Shield:
                                shieldPoints += delta;
                                break;
                            case StatTarget.Damage:
                                damagePoints += delta;
                                break;
                            case StatTarget.MagicResist:
                                magicResistPoints += delta;
                                break;
                        }

                        if (playerUI != null)
                            playerUI.SetDisplayedCombatValues(
                                healthPoints, healthMultiplier,
                                shieldPoints, shieldMultiplier,
                                damagePoints, damageMultiplier,
                                magicResistPoints, magicResistMultiplier);
                    });
                }
            }

            if (multipliersBySlot.TryGetValue(slot, out List<PendingMultiplier> pendingMultipliers))
            {
                foreach (PendingMultiplier pending in pendingMultipliers)
                {
                    yield return AnimateMultiplierPopup(
                        itemVisual.transform as RectTransform,
                        pending.Value,
                        pending.Enchantment);

                    damageMultiplier += pending.Value;
                    if (playerUI != null)
                    {
                        playerUI.SetDisplayedCombatValues(
                            healthPoints, healthMultiplier,
                            shieldPoints, shieldMultiplier,
                            damagePoints, damageMultiplier,
                            magicResistPoints, magicResistMultiplier);
                        yield return StatTransferEffect.PulseCounter(playerUI.DamageStatText);
                    }
                }
            }

            if (itemData.abilities != null)
            {
                for (int abilityIndex = 0; abilityIndex < itemData.abilities.Count; abilityIndex++)
                {
                    AbilityData ability = itemData.abilities[abilityIndex];
                    if (ability == null)
                        continue;

                    PendingAbility pendingAbility = new PendingAbility { Data = ability };
                    yield return AnimateAbilityToCenter(
                        itemVisual.transform as RectTransform,
                        pendingAbility);
                    yield return ResolvePendingAbility(pendingAbility);
                }
            }
        }

        if (!hasAppliedFirstAttackHealth && playerUI != null)
        {
            int totalHealth = Mathf.RoundToInt(Mathf.Max(0f, healthPoints * healthMultiplier));
            playerUI.AddBattleHealth(totalHealth);
            hasAppliedFirstAttackHealth = true;
        }

        if (playerUI != null)
        {
            playerUI.SetCombatDefenses(
                shieldPoints * shieldMultiplier,
                magicResistPoints * magicResistMultiplier);
            playerUI.SetDisplayedCombatValues(
                healthPoints, healthMultiplier,
                shieldPoints, shieldMultiplier,
                damagePoints, damageMultiplier,
                magicResistPoints, magicResistMultiplier,
                true);
        }

        yield return new WaitForSecondsRealtime(0.5f);

        int totalDamage = Mathf.RoundToInt(Mathf.Max(0f, damagePoints * damageMultiplier));
        if (bossUI != null && totalDamage > 0)
            yield return bossUI.PlayDamageEffect(totalDamage);

        if (bossUI != null && bossUI.CurrentHealth > 0 && playerUI != null && bossUI.Data != null)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            yield return playerUI.PlayDamageEffect(bossUI.Data.AttackDamage, bossUI.Data.AttackDamageType);
            yield return ResolveAbilitiesAfterEnemyAttack(bossUI);
        }

        if (playerUI != null)
        {
            playerUI.SetCombatDefenses(0f, 0f);
            playerUI.SetDisplayedCombatValues(
                0f, playerData != null ? playerData.HealthMultiplier : 1f,
                0f, playerData != null ? playerData.ShieldMultiplier : 1f,
                0f, playerData != null ? playerData.DamageMultiplier : 1f,
                0f, playerData != null ? playerData.MagicResistMultiplier : 1f);
        }

        UpdateGoldUI();

        if (startButton != null)
            startButton.interactable = true;
        isStartingBattle = false;
    }

    private static Dictionary<AnvilSlotUI, List<PendingMultiplier>> CollectMultipliersByTarget(
        AnvilSlotUI[] slots,
        int rowCount,
        int columnCount)
    {
        Dictionary<AnvilSlotUI, List<PendingMultiplier>> result = new();

        foreach (AnvilSlotUI sourceSlot in slots)
        {
            ItemInstance sourceInstance = sourceSlot != null && sourceSlot.CurrentItem != null
                ? sourceSlot.CurrentItem.ItemInstance
                : null;
            if (sourceInstance == null || sourceInstance.enchantments == null)
                continue;

            foreach (EnchantmentInstance enchantment in sourceInstance.enchantments)
            {
                if (enchantment == null || enchantment.data == null)
                    continue;

                if (enchantment.data.effectKind != EnchantmentData.EffectKind.BattleMultiplier)
                    continue;
                float bonus = enchantment.data.damageMultiplierPerMatch;
                if (Mathf.Approximately(bonus, 0f))
                    continue;

                int appliedMatches = 0;
                int maximumMatches = Mathf.Max(1, enchantment.data.maximumMatches);
                foreach (AnvilSlotUI targetSlot in slots)
                {
                    if (appliedMatches >= maximumMatches)
                        break;

                    DraggedItemVisual targetItem = targetSlot != null ? targetSlot.CurrentItem : null;
                    ItemData targetData = targetItem != null ? targetItem.ItemData : null;
                    if (targetData == null || targetData.itemGroups == null ||
                        !targetData.itemGroups.Contains(enchantment.data.requiredGroup))
                        continue;

                    if (!GridPlacementRuleUtility.IsSlotAllowed(
                            enchantment.targetRule,
                            targetSlot.Row,
                            targetSlot.Column,
                            rowCount,
                            columnCount))
                        continue;

                    if (!result.TryGetValue(targetSlot, out List<PendingMultiplier> targetMultipliers))
                    {
                        targetMultipliers = new List<PendingMultiplier>();
                        result.Add(targetSlot, targetMultipliers);
                    }

                    targetMultipliers.Add(new PendingMultiplier(bonus, enchantment.data));
                    appliedMatches++;
                }
            }
        }

        return result;
    }


    private IEnumerator AnimateMultiplierPopup(RectTransform source, float value, EnchantmentData enchantment)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null || source == null)
            yield break;

        GameObject popupObject = new GameObject(
            "MultiplierPopup_" + (enchantment != null ? enchantment.id : "Enchantment"),
            typeof(RectTransform),
            typeof(CanvasGroup));
        popupObject.transform.SetParent(canvas.transform, false);
        popupObject.transform.SetAsLastSibling();

        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        popupRect.sizeDelta = new Vector2(104f, 104f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.position = source.TransformPoint(new Vector3(0f, source.rect.height * 0.5f + 48f, 0f));
        popupRect.localScale = Vector3.zero;

        GameObject diamondObject = new GameObject(
            "MultiplierDiamond",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline));
        diamondObject.transform.SetParent(popupObject.transform, false);
        RectTransform diamondRect = diamondObject.GetComponent<RectTransform>();
        diamondRect.anchorMin = diamondRect.anchorMax = new Vector2(0.5f, 0.5f);
        diamondRect.sizeDelta = new Vector2(72f, 72f);
        diamondRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image diamondImage = diamondObject.GetComponent<Image>();
        diamondImage.color = new Color(0.4f, 0.12f, 0.82f, 0.96f);
        diamondImage.raycastTarget = false;
        Outline diamondOutline = diamondObject.GetComponent<Outline>();
        diamondOutline.effectColor = new Color(0.25f, 1f, 0.9f, 1f);
        diamondOutline.effectDistance = new Vector2(3f, -3f);

        GameObject textObject = new GameObject(
            "MultiplierValue",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(popupObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI popupText = textObject.GetComponent<TextMeshProUGUI>();
        popupText.text = $"+{value:0.##}x";
        popupText.fontSize = 34f;
        popupText.fontStyle = FontStyles.Bold;
        popupText.alignment = TextAlignmentOptions.Center;
        popupText.color = new Color(0.75f, 1f, 0.92f, 1f);
        popupText.outlineColor = new Color32(35, 0, 65, 255);
        popupText.outlineWidth = 0.25f;
        popupText.raycastTarget = false;

        CanvasGroup popupGroup = popupObject.GetComponent<CanvasGroup>();
        Vector3 startPosition = popupRect.position;
        const float duration = 1.05f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float appear = Mathf.Clamp01(t / 0.18f);
            float pulse = 1f + Mathf.Sin(t * Mathf.PI * 6f) * 0.09f * (1f - t);
            popupRect.localScale = Vector3.one * appear * pulse;
            popupRect.position = startPosition + canvas.transform.TransformVector(Vector3.up * (48f * t));
            diamondRect.localRotation = Quaternion.Euler(0f, 0f, 45f + 260f * t);
            diamondImage.color = Color.Lerp(
                new Color(0.4f, 0.12f, 0.82f, 0.96f),
                new Color(0.05f, 0.72f, 0.62f, 0.96f),
                t);
            popupGroup.alpha = t < 0.72f ? 1f : 1f - ((t - 0.72f) / 0.28f);
            yield return null;
        }

        if (popupObject != null)
            Destroy(popupObject);
    }

    private IEnumerator AnimateAbilityToCenter(
        RectTransform source,
        PendingAbility pendingAbility)
    {
        EnsureAbilityCombatLayer();
        if (abilityCombatLayer == null || source == null || pendingAbility == null || pendingAbility.Data == null)
            yield break;

        RectTransform visual = CreateAbilityVisual(
            abilityCombatLayer,
            "PendingAbility_" + pendingAbility.Data.id,
            pendingAbility.Data,
            new Vector2(220f, 220f),
            true);
        pendingAbility.Visual = visual;
        visual.anchoredPosition = abilityCombatLayer.InverseTransformPoint(source.position);
        visual.localScale = Vector3.one * 0.28f;
        CanvasGroup entranceGroup = visual.gameObject.AddComponent<CanvasGroup>();
        entranceGroup.blocksRaycasts = false;
        entranceGroup.alpha = 0f;

        Vector2 target = new Vector2(-85f, 0f);
        Vector2 start = visual.anchoredPosition;
        float elapsed = 0f;
        const float duration = 0.65f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = AbilityEase(t);
            Vector2 arc = Vector2.up * Mathf.Pow(Mathf.Sin(eased * Mathf.PI), 2f) * 55f;
            visual.anchoredPosition = Vector2.LerpUnclamped(start, target, eased) + arc;
            visual.localScale = Vector3.one * Mathf.Lerp(0.28f, 1f, eased);
            visual.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-12f, 0f, eased));
            entranceGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
            yield return null;
        }

        visual.anchoredPosition = target;
        visual.localScale = Vector3.one;
        visual.localEulerAngles = Vector3.zero;
    }

    private IEnumerator ResolvePendingAbility(PendingAbility pendingAbility)
    {
        if (pendingAbility == null || pendingAbility.Data == null || pendingAbility.Visual == null)
            yield break;

        EnsureAbilityCombatLayer();
        RectTransform visual = pendingAbility.Visual;
        Vector2 dicePosition = new Vector2(145f, 0f);
        yield return new WaitForSecondsRealtime(0.3f);

        int roll = Random.Range(1, 21);
        yield return AnimateD20Roll(pendingAbility.Data, roll, dicePosition);

        if (!pendingAbility.Data.IsActivatedBy(roll))
        {
            yield return AnimateFailedAbility(visual);
            yield break;
        }

        ActiveAbilityEffect existingEffect = FindActiveAbility(pendingAbility.Data);
        if (existingEffect != null && existingEffect.Visual != null)
        {
            Transform abilityName = visual.Find("Name");
            if (abilityName != null)
                abilityName.gameObject.SetActive(false);
            yield return MoveAbilityVisual(
                visual,
                visual.anchoredPosition,
                existingEffect.Visual.anchoredPosition,
                0.48f);
            existingEffect.RemainingEnemyAttacks += pendingAbility.Data.durationInEnemyAttacks;
            SetAbilityTurnsLabel(existingEffect.Visual, existingEffect.RemainingEnemyAttacks);
            Destroy(visual.gameObject);
            yield return PulseAbilityVisual(
                existingEffect.Visual,
                new Color(0.25f, 1f, 0.42f, 1f));
            yield break;
        }

        ActiveAbilityEffect activeEffect = new ActiveAbilityEffect
        {
            Data = pendingAbility.Data,
            RemainingEnemyAttacks = pendingAbility.Data.durationInEnemyAttacks,
            Visual = visual
        };
        activeAbilityEffects.Add(activeEffect);
        Transform stagedName = visual.Find("Name");
        if (stagedName != null)
            stagedName.gameObject.SetActive(false);
        Vector2 activePosition = GetActiveAbilityPosition(activeAbilityEffects.Count - 1);
        yield return MoveAbilityVisual(visual, visual.anchoredPosition, activePosition, 0.58f);
        visual.name = "ActiveAbility_" + pendingAbility.Data.id;
        SetAbilityTurnsLabel(visual, activeEffect.RemainingEnemyAttacks);
        yield return PulseAbilityVisual(visual, new Color(0.25f, 1f, 0.42f, 1f));
    }

    private ActiveAbilityEffect FindActiveAbility(AbilityData ability)
    {
        if (ability == null)
            return null;

        for (int i = 0; i < activeAbilityEffects.Count; i++)
        {
            ActiveAbilityEffect active = activeAbilityEffects[i];
            if (active == null || active.Data == null)
                continue;

            if (active.Data == ability ||
                (!string.IsNullOrWhiteSpace(active.Data.id) && active.Data.id == ability.id))
                return active;
        }

        return null;
    }

    private IEnumerator ResolveAbilitiesAfterEnemyAttack(BossUI bossUI)
    {
        if (bossUI == null || activeAbilityEffects.Count == 0)
            yield break;

        for (int i = activeAbilityEffects.Count - 1; i >= 0; i--)
        {
            ActiveAbilityEffect effect = activeAbilityEffects[i];
            if (effect == null || effect.Data == null)
            {
                activeAbilityEffects.RemoveAt(i);
                continue;
            }

            if (effect.Visual != null)
                yield return PulseAbilityVisual(effect.Visual, new Color(1f, 0.18f, 0.15f, 1f));

            if (effect.Data.damageAfterEnemyAttack > 0 && bossUI.CurrentHealth > 0)
                yield return bossUI.PlayDamageEffect(effect.Data.damageAfterEnemyAttack);

            effect.RemainingEnemyAttacks--;
            SetAbilityTurnsLabel(effect.Visual, effect.RemainingEnemyAttacks);
            if (effect.RemainingEnemyAttacks > 0)
                continue;

            if (effect.Visual != null)
                yield return FadeAndDestroyAbility(effect.Visual);
            activeAbilityEffects.RemoveAt(i);
        }

        RepositionActiveAbilities();
    }

    private void EnsureAbilityCombatLayer()
    {
        if (abilityCombatLayer != null)
            return;

        Canvas sourceCanvas = FindFirstObjectByType<Canvas>();
        Canvas canvas = sourceCanvas != null ? sourceCanvas.rootCanvas : null;
        if (canvas == null)
            return;

        Transform existing = canvas.transform.Find("AbilityCombatLayer");
        GameObject layerObject;
        if (existing != null)
        {
            layerObject = existing.gameObject;
        }
        else
        {
            layerObject = new GameObject("AbilityCombatLayer", typeof(RectTransform), typeof(Canvas));
            layerObject.layer = canvas.gameObject.layer;
            layerObject.transform.SetParent(canvas.transform, false);
        }

        abilityCombatLayer = layerObject.GetComponent<RectTransform>();
        abilityCombatLayer.anchorMin = Vector2.zero;
        abilityCombatLayer.anchorMax = Vector2.one;
        abilityCombatLayer.offsetMin = Vector2.zero;
        abilityCombatLayer.offsetMax = Vector2.zero;
        Canvas layerCanvas = layerObject.GetComponent<Canvas>();
        if (layerCanvas == null)
            layerCanvas = layerObject.AddComponent<Canvas>();
        layerCanvas.overrideSorting = true;
        layerCanvas.sortingOrder = 19000;
    }

    private RectTransform CreateAbilityVisual(
        Transform parent,
        string objectName,
        AbilityData ability,
        Vector2 size,
        bool showName)
    {
        GameObject visualObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline));
        visualObject.layer = parent.gameObject.layer;
        visualObject.transform.SetParent(parent, false);
        RectTransform rect = visualObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = visualObject.GetComponent<Image>();
        image.sprite = ability.icon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        Outline outline = visualObject.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.2f, 0.16f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        if (showName)
        {
            GameObject nameObject = new GameObject(
                "Name",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            nameObject.transform.SetParent(rect, false);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0f);
            nameRect.anchorMax = new Vector2(0.5f, 0f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0f, -5f);
            nameRect.sizeDelta = new Vector2(300f, 36f);
            TextMeshProUGUI label = nameObject.GetComponent<TextMeshProUGUI>();
            label.text = ability.abilityName;
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.outlineWidth = 0.2f;
            label.raycastTarget = false;
        }

        return rect;
    }

    private IEnumerator AnimateD20Roll(AbilityData ability, int finalRoll, Vector2 position)
    {
        Sprite diceSprite = Resources.Load<Sprite>("Images/Items/dado");
        GameObject diceObject = new GameObject(
            "D20Roll",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        diceObject.transform.SetParent(abilityCombatLayer, false);
        RectTransform diceRect = diceObject.GetComponent<RectTransform>();
        diceRect.anchorMin = diceRect.anchorMax = new Vector2(0.5f, 0.5f);
        diceRect.sizeDelta = new Vector2(125f, 125f);
        diceRect.anchoredPosition = position;
        Image diceImage = diceObject.GetComponent<Image>();
        diceImage.sprite = diceSprite;
        diceImage.preserveAspect = true;
        diceImage.color = new Color(0.7f, 1f, 0.56f, 1f);
        diceImage.raycastTarget = false;

        GameObject numberObject = new GameObject(
            "Result",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        numberObject.transform.SetParent(diceRect, false);
        RectTransform numberRect = numberObject.GetComponent<RectTransform>();
        numberRect.anchorMin = Vector2.zero;
        numberRect.anchorMax = Vector2.one;
        numberRect.offsetMin = Vector2.zero;
        numberRect.offsetMax = Vector2.zero;
        TextMeshProUGUI numberText = numberObject.GetComponent<TextMeshProUGUI>();
        numberText.fontSize = 42f;
        numberText.fontStyle = FontStyles.Bold;
        numberText.alignment = TextAlignmentOptions.Center;
        numberText.color = Color.white;
        numberText.outlineWidth = 0.28f;
        numberText.raycastTarget = false;

        CanvasGroup diceGroup = diceObject.GetComponent<CanvasGroup>();
        diceGroup.blocksRaycasts = false;
        diceGroup.alpha = 0f;
        Color neutral = new Color(0.82f, 0.9f, 1f, 1f);
        diceImage.color = neutral;
        float elapsed = 0f;
        float nextNumberChange = 0f;
        int faceIndex = 0;
        const float duration = 1.15f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            if (elapsed >= nextNumberChange)
            {
                nextNumberChange = elapsed + Mathf.Lerp(0.045f, 0.22f, t * t);
                // Cosmetic faces must not consume the gameplay random sequence.
                numberText.text = (1 + (faceIndex++ * 7 + finalRoll) % 20).ToString();
            }
            float angle = 720f * eased;
            diceRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            numberRect.localRotation = Quaternion.Euler(0f, 0f, -angle);
            float enter = AbilityEase(Mathf.Clamp01(t / 0.22f));
            float wobble = Mathf.Sin(t * Mathf.PI * 6f) * Mathf.Pow(1f - t, 2f);
            diceRect.localScale = Vector3.one * (Mathf.Lerp(0.65f, 1f, enter) + wobble * 0.045f);
            diceRect.anchoredPosition = position + new Vector2(wobble * 5f, 20f * (1f - enter));
            diceGroup.alpha = enter;
            yield return null;
        }

        numberText.text = finalRoll.ToString();
        diceRect.localRotation = Quaternion.identity;
        numberRect.localRotation = Quaternion.identity;
        diceRect.localScale = Vector3.one;
        diceRect.anchoredPosition = position;
        bool success = ability.IsActivatedBy(finalRoll);
        Color resultColor = success
            ? new Color(0.28f, 1f, 0.42f, 1f)
            : new Color(1f, 0.2f, 0.18f, 1f);

        GameObject requirementObject = new GameObject(
            "Requirement",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        requirementObject.transform.SetParent(diceRect, false);
        RectTransform requirementRect = requirementObject.GetComponent<RectTransform>();
        requirementRect.anchorMin = requirementRect.anchorMax = new Vector2(0.5f, 0f);
        requirementRect.pivot = new Vector2(0.5f, 1f);
        requirementRect.anchoredPosition = new Vector2(0f, -8f);
        requirementRect.sizeDelta = new Vector2(220f, 30f);
        TextMeshProUGUI requirement = requirementObject.GetComponent<TextMeshProUGUI>();
        requirement.text = $"{ability.abilityName}: {finalRoll} / {ability.minimumD20Roll}";
        requirement.fontSize = 18f;
        requirement.fontStyle = FontStyles.Bold;
        requirement.alignment = TextAlignmentOptions.Center;
        requirement.color = success ? new Color(0.45f, 1f, 0.55f, 1f) : new Color(1f, 0.35f, 0.3f, 1f);
        requirement.outlineWidth = 0.2f;
        requirement.raycastTarget = false;

        elapsed = 0f;
        const float revealDuration = 0.65f;
        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / revealDuration);
            float pulse = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.65f));
            diceRect.localScale = Vector3.one * (1f + pulse * pulse * 0.16f);
            diceImage.color = Color.Lerp(neutral, resultColor, AbilityEase(Mathf.Clamp01(t / 0.3f)));
            requirement.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.25f));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.22f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = AbilityEase(Mathf.Clamp01(elapsed / 0.22f));
            diceGroup.alpha = 1f - t;
            diceRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.88f, t);
            diceRect.anchoredPosition = position + Vector2.down * (14f * t);
            yield return null;
        }
        Destroy(diceObject);
    }

    private static float AbilityEase(float t)
    {
        return t * t * t * (t * (6f * t - 15f) + 10f);
    }

    private IEnumerator MoveAbilityVisual(RectTransform visual, Vector2 start, Vector2 end, float duration)
    {
        if (visual == null) yield break;
        Vector2 startSize = visual.sizeDelta;
        float elapsed = 0f;
        while (elapsed < duration && visual != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = AbilityEase(Mathf.Clamp01(elapsed / duration));
            visual.anchoredPosition = Vector2.LerpUnclamped(start, end, t)
                + Vector2.up * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2f) * 45f;
            visual.sizeDelta = Vector2.Lerp(startSize, new Vector2(36f, 36f), t);
            visual.localScale = Vector3.one;
            yield return null;
        }
        if (visual == null) yield break;
        visual.anchoredPosition = end;
        visual.sizeDelta = new Vector2(36f, 36f);
        visual.localScale = Vector3.one;
    }

    private IEnumerator PulseAbilityVisual(RectTransform visual, Color color)
    {
        if (visual == null)
            yield break;
        Image image = visual.GetComponent<Image>();
        Color originalColor = image != null ? image.color : Color.white;
        float elapsed = 0f;
        const float duration = 0.42f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2f);
            visual.localScale = Vector3.one * (1f + pulse * 0.2f);
            if (image != null)
                image.color = Color.Lerp(originalColor, color, pulse);
            yield return null;
        }
        visual.localScale = Vector3.one;
        if (image != null)
            image.color = originalColor;
    }

    private IEnumerator AnimateFailedAbility(RectTransform visual)
    {
        if (visual == null)
            yield break;
        CanvasGroup group = visual.GetComponent<CanvasGroup>();
        if (group == null)
            group = visual.gameObject.AddComponent<CanvasGroup>();
        Vector2 origin = visual.anchoredPosition;
        float elapsed = 0f;
        const float duration = 0.45f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fade = AbilityEase(t);
            visual.anchoredPosition = origin + Vector2.right * Mathf.Sin(t * Mathf.PI * 4f) * 7f * (1f - t) * (1f - t)
                + Vector2.down * (18f * fade);
            visual.localScale = Vector3.one * (1f - fade * 0.15f);
            group.alpha = 1f - fade;
            yield return null;
        }
        Destroy(visual.gameObject);
    }

    private IEnumerator FadeAndDestroyAbility(RectTransform visual)
    {
        CanvasGroup group = visual.GetComponent<CanvasGroup>();
        if (group == null)
            group = visual.gameObject.AddComponent<CanvasGroup>();
        float elapsed = 0f;
        const float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = AbilityEase(t);
            group.alpha = 1f - eased;
            visual.localScale = Vector3.one * (1f + eased * 0.1f);
            yield return null;
        }
        Destroy(visual.gameObject);
    }

    private void SetAbilityTurnsLabel(RectTransform visual, int turns)
    {
        if (visual == null)
            return;
        Transform existing = visual.Find("Turns");
        TextMeshProUGUI label;
        if (existing == null)
        {
            GameObject labelObject = new GameObject(
                "Turns",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(visual, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(1f, 0f);
            labelRect.anchoredPosition = new Vector2(2f, -2f);
            labelRect.sizeDelta = new Vector2(22f, 18f);
            label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 12f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.outlineWidth = 0.3f;
            label.raycastTarget = false;
        }
        else
        {
            label = existing.GetComponent<TextMeshProUGUI>();
        }
        if (label != null)
            label.text = Mathf.Max(0, turns).ToString();
    }

    private Vector2 GetActiveAbilityPosition(int index)
    {
        const int columns = 10;
        const float abilitySize = 36f;
        int column = index % columns;
        int row = index / columns;
        float firstCenterX = -abilityCombatLayer.rect.width * 0.205f;

        BossUI bossUI = FindFirstObjectByType<BossUI>();
        RectTransform bossRect = bossUI != null ? bossUI.transform as RectTransform : null;
        RectTransform innerBossRect = bossRect != null
            ? bossRect.Find("PortraitFrame") as RectTransform
            : null;
        if (innerBossRect != null)
            bossRect = innerBossRect;
        if (bossRect != null)
        {
            Vector3[] corners = new Vector3[4];
            bossRect.GetWorldCorners(corners);
            float bossLeftX = abilityCombatLayer.InverseTransformPoint(corners[0]).x;
            firstCenterX = bossLeftX + abilitySize * 0.5f;
        }

        return new Vector2(
            firstCenterX + column * 42f,
            abilityCombatLayer.rect.height * 0.38f - row * 42f);
    }

    private void RepositionActiveAbilities()
    {
        for (int i = 0; i < activeAbilityEffects.Count; i++)
        {
            if (activeAbilityEffects[i].Visual != null)
                activeAbilityEffects[i].Visual.anchoredPosition = GetActiveAbilityPosition(i);
        }
    }

    private static StatTarget GetStatTarget(StatType statType)
    {
        switch (statType)
        {
            case StatType.HP:
                return StatTarget.Health;
            case StatType.Armor:
                return StatTarget.Shield;
            case StatType.MagicResist:
                return StatTarget.MagicResist;
            case StatType.CriticalDamage:
            case StatType.SlashingDamage:
            case StatType.PiercingDamage:
            case StatType.BludgeoningDamage:
            case StatType.CounterattackDamage:
                return StatTarget.Damage;
            default:
                return StatTarget.None;
        }
    }

    private static Color GetStatColor(StatTarget target)
    {
        switch (target)
        {
            case StatTarget.Health:
                return new Color(0.86f, 0.16f, 0.2f, 1f);
            case StatTarget.Shield:
                return new Color(0.12f, 0.48f, 0.88f, 1f);
            case StatTarget.Damage:
                return new Color(0.95f, 0.52f, 0.05f, 1f);
            case StatTarget.MagicResist:
                return new Color(0.58f, 0.27f, 0.86f, 1f);
            default:
                return Color.gray;
        }
    }

    private enum StatTarget
    {
        None,
        Health,
        Shield,
        Damage,
        MagicResist
    }
}
