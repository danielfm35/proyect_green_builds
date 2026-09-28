using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AnvilCraftManager : MonoBehaviour
{
    [SerializeField] private List<AnvilSlotUI> slots = new();
    [SerializeField] private List<AnvilEffectData> blessings = new();
    [SerializeField] private List<AnvilEffectData> curses = new();
    [SerializeField]
    [Tooltip("Permite generar bendiciones y maldiciones aleatorias después de usar el yunque.")]
    private bool enableRandomAnvilEffects;
    [SerializeField] private bool enableDebugLogs = false;

    private static AnvilCraftManager instance;
    public static bool IsResolvingCraftSequence { get; private set; }
    private bool effectsLoaded;
    private readonly Dictionary<Vector2Int, AnvilSlotUI> slotMap = new();
    private class PendingAnvilEffect
    {
        public AnvilEffectData data;
        public AnvilSlotUI slot;
        public GridPlacementRule targetRule;
    }

    private static readonly Vector2Int[] NeighborOffsets =
    {
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1)
    };

    private void Awake()
    {
        instance = this;
        AutoPopulateSlotsIfNeeded();
        BuildMap();
        EnsureEffectsLoaded();
        RefreshEffectTargetHighlights();
        LogDebug($"Awake ejecutado. Slots registrados: {slotMap.Count}.");
    }

    private void OnEnable()
    {
        instance = this;
    }

    private void OnDisable()
    {
        if (instance == this)
            IsResolvingCraftSequence = false;
    }

    public static AnvilCraftManager GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<AnvilCraftManager>();
        if (instance != null)
            return instance;

        GameObject managerObject = new GameObject("AnvilCraftManager_Auto");
        instance = managerObject.AddComponent<AnvilCraftManager>();
        return instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void WarmUpWhenAnvilExists()
    {
        if (instance != null)
            return;

        if (FindFirstObjectByType<AnvilSlotUI>() == null)
            return;

        GetOrCreate();
    }

    private void BuildMap()
    {
        AutoAssignCoordinatesFromLayout();
        slotMap.Clear();

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                continue;

            Vector2Int key = new Vector2Int(slots[i].Row, slots[i].Column);
            if (!slotMap.ContainsKey(key))
            {
                slotMap.Add(key, slots[i]);
            }
        }
    }

    private void AutoPopulateSlotsIfNeeded()
    {
        if (slots.Count > 0)
            return;

        AnvilSlotUI[] discoveredSlots = FindObjectsByType<AnvilSlotUI>(FindObjectsSortMode.None);
        slots.AddRange(discoveredSlots);
    }

    private void AutoAssignCoordinatesFromLayout()
    {
        if (slots.Count == 0)
            return;

        List<AnvilSlotUI> validSlots = new();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
                validSlots.Add(slots[i]);
        }

        if (validSlots.Count == 0)
            return;

        const float tolerance = 5f;

        List<float> rowAnchors = validSlots
            .Select(slot => ((RectTransform)slot.transform).anchoredPosition.y)
            .OrderByDescending(y => y)
            .ToList();

        List<float> colAnchors = validSlots
            .Select(slot => ((RectTransform)slot.transform).anchoredPosition.x)
            .OrderBy(x => x)
            .ToList();

        List<float> distinctRows = BuildDistinctCoordinateList(rowAnchors, tolerance);
        List<float> distinctColumns = BuildDistinctCoordinateList(colAnchors, tolerance);

        for (int i = 0; i < validSlots.Count; i++)
        {
            RectTransform rect = (RectTransform)validSlots[i].transform;
            int row = FindClosestIndex(distinctRows, rect.anchoredPosition.y);
            int column = FindClosestIndex(distinctColumns, rect.anchoredPosition.x);
            validSlots[i].SetCoordinates(row, column);
        }

        LogDebug($"Coordenadas autoasignadas. Filas={distinctRows.Count}, Columnas={distinctColumns.Count}, Slots={validSlots.Count}.");
    }

    private List<float> BuildDistinctCoordinateList(List<float> sortedValues, float tolerance)
    {
        List<float> distinct = new();

        for (int i = 0; i < sortedValues.Count; i++)
        {
            float value = sortedValues[i];
            bool alreadyGrouped = false;

            for (int j = 0; j < distinct.Count; j++)
            {
                if (Mathf.Abs(distinct[j] - value) <= tolerance)
                {
                    alreadyGrouped = true;
                    break;
                }
            }

            if (!alreadyGrouped)
                distinct.Add(value);
        }

        return distinct;
    }

    private int FindClosestIndex(List<float> values, float target)
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < values.Count; i++)
        {
            float distance = Mathf.Abs(values[i] - target);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public void TryCraftFromSlot(AnvilSlotUI originSlot)
    {
        if (IsResolvingCraftSequence)
        {
            LogDebug("TryCraftFromSlot cancelado: secuencia de crafteo en progreso.");
            return;
        }

        AutoPopulateSlotsIfNeeded();
        BuildMap();

        if (originSlot == null || !originSlot.HasItem)
        {
            LogDebug("TryCraftFromSlot cancelado: originSlot nulo o vacio.");
            return;
        }

        Vector2Int originPos = new Vector2Int(originSlot.Row, originSlot.Column);
        LogDebug($"Intentando craftear desde slot ({originSlot.Row}, {originSlot.Column}) con {DescribeItem(originSlot.CurrentItem)}.");

        for (int i = 0; i < NeighborOffsets.Length; i++)
        {
            if (!slotMap.TryGetValue(originPos + NeighborOffsets[i], out AnvilSlotUI neighborSlot))
                continue;

            if (!neighborSlot.HasItem)
            {
                LogDebug($"Vecino ({originPos.x + NeighborOffsets[i].x}, {originPos.y + NeighborOffsets[i].y}) vacio.");
                continue;
            }

            if (TryResolvePair(originSlot, neighborSlot))
                return;
        }

        LogDebug($"No se encontro crafteo valido para el slot ({originSlot.Row}, {originSlot.Column}).");
    }

    public bool CanPlaceItemInSlot(ItemInstance itemInstance, AnvilSlotUI targetSlot)
    {
        if (IsResolvingCraftSequence)
            return false;

        AutoPopulateSlotsIfNeeded();
        BuildMap();

        if (itemInstance == null || targetSlot == null)
            return false;

        GetGridSize(out int rowCount, out int columnCount);
        return GridPlacementRuleUtility.IsSlotAllowed(
            itemInstance.placementRule,
            targetSlot.Row,
            targetSlot.Column,
            rowCount,
            columnCount
        );
    }

    private bool TryResolvePair(AnvilSlotUI firstSlot, AnvilSlotUI secondSlot)
    {
        if (firstSlot == null || secondSlot == null)
            return false;

        DraggedItemVisual firstItem = firstSlot.CurrentItem;
        DraggedItemVisual secondItem = secondSlot.CurrentItem;

        if (firstItem == null || secondItem == null)
        {
            LogDebug("TryResolvePair cancelado: falta alguno de los visuals.");
            return false;
        }

        if (firstItem.ItemInstance == null || secondItem.ItemInstance == null)
        {
            LogDebug("TryResolvePair cancelado: falta alguno de los ItemInstance.");
            return false;
        }

        if (firstItem.ItemInstance.data == null || secondItem.ItemInstance.data == null)
        {
            LogDebug("TryResolvePair cancelado: falta alguno de los ItemData.");
            return false;
        }

        GetOrderedPair(firstSlot, secondSlot, out AnvilSlotUI keepSlot, out AnvilSlotUI consumeSlot);
        DraggedItemVisual keepItem = keepSlot.CurrentItem;
        DraggedItemVisual consumeItem = consumeSlot.CurrentItem;

        LogDebug(
            $"Evaluando pareja keep ({keepSlot.Row}, {keepSlot.Column}) {DescribeItem(keepItem)} " +
            $"consume ({consumeSlot.Row}, {consumeSlot.Column}) {DescribeItem(consumeItem)}."
        );

        if (!HasCraftConnection(keepSlot, keepItem, consumeSlot, consumeItem))
        {
            LogDebug("La pareja no tiene una linea roja apuntando al vecino.");
            return false;
        }

        return ResolveCraft(keepSlot, keepItem, consumeSlot, consumeItem);
    }

    private void GetOrderedPair(
        AnvilSlotUI firstSlot,
        AnvilSlotUI secondSlot,
        out AnvilSlotUI keepSlot,
        out AnvilSlotUI consumeSlot
    )
    {
        DraggedItemVisual firstItem = firstSlot.CurrentItem;
        DraggedItemVisual secondItem = secondSlot.CurrentItem;

        if (firstItem != null && secondItem != null &&
            firstItem.ItemInstance != null && secondItem.ItemInstance != null &&
            firstItem.ItemInstance.rarity != secondItem.ItemInstance.rarity)
        {
            if (firstItem.ItemInstance.rarity > secondItem.ItemInstance.rarity)
            {
                keepSlot = firstSlot;
                consumeSlot = secondSlot;
                return;
            }

            keepSlot = secondSlot;
            consumeSlot = firstSlot;
            return;
        }

        if (firstSlot.Row < secondSlot.Row ||
            (firstSlot.Row == secondSlot.Row && firstSlot.Column <= secondSlot.Column))
        {
            keepSlot = firstSlot;
            consumeSlot = secondSlot;
            return;
        }

        keepSlot = secondSlot;
        consumeSlot = firstSlot;
    }

    private bool HasCraftConnection(
        AnvilSlotUI keepSlot,
        DraggedItemVisual keepItem,
        AnvilSlotUI consumeSlot,
        DraggedItemVisual consumeItem
    )
    {
        int rowDiff = consumeSlot.Row - keepSlot.Row;
        int colDiff = consumeSlot.Column - keepSlot.Column;

        if (rowDiff == 0 && colDiff == 1)
        {
            bool connected = keepItem.ItemInstance.crafterSide == CrafterSide.Right ||
                             consumeItem.ItemInstance.crafterSide == CrafterSide.Left;
            LogDebug(
                $"Conexion horizontal: keepSide={keepItem.ItemInstance.crafterSide}, " +
                $"consumeSide={consumeItem.ItemInstance.crafterSide}, resultado={connected}."
            );
            return connected;
        }

        if (rowDiff == 0 && colDiff == -1)
        {
            bool connected = keepItem.ItemInstance.crafterSide == CrafterSide.Left ||
                             consumeItem.ItemInstance.crafterSide == CrafterSide.Right;
            LogDebug(
                $"Conexion horizontal invertida: keepSide={keepItem.ItemInstance.crafterSide}, " +
                $"consumeSide={consumeItem.ItemInstance.crafterSide}, resultado={connected}."
            );
            return connected;
        }

        if (rowDiff == 1 && colDiff == 0)
        {
            bool connected = keepItem.ItemInstance.crafterSide == CrafterSide.Bottom ||
                             consumeItem.ItemInstance.crafterSide == CrafterSide.Top;
            LogDebug(
                $"Conexion vertical: keepSide={keepItem.ItemInstance.crafterSide}, " +
                $"consumeSide={consumeItem.ItemInstance.crafterSide}, resultado={connected}."
            );
            return connected;
        }

        if (rowDiff == -1 && colDiff == 0)
        {
            bool connected = keepItem.ItemInstance.crafterSide == CrafterSide.Top ||
                             consumeItem.ItemInstance.crafterSide == CrafterSide.Bottom;
            LogDebug(
                $"Conexion vertical invertida: keepSide={keepItem.ItemInstance.crafterSide}, " +
                $"consumeSide={consumeItem.ItemInstance.crafterSide}, resultado={connected}."
            );
            return connected;
        }

        LogDebug($"Slots no adyacentes para crafteo: rowDiff={rowDiff}, colDiff={colDiff}.");
        return false;
    }

    private bool ResolveCraft(
        AnvilSlotUI slotA,
        DraggedItemVisual itemA,
        AnvilSlotUI slotB,
        DraggedItemVisual itemB
    )
    {
        ItemData dataA = itemA.ItemInstance.data;
        ItemData dataB = itemB.ItemInstance.data;

        if (dataA == null || dataB == null)
        {
            LogDebug("ResolveCraft cancelado: dataA o dataB es null.");
            return false;
        }

        if (dataA.id == dataB.id)
        {
            LogDebug($"Items iguales detectados: {dataA.id}. Se intentara upgrade de rareza.");
            return TryUpgradeSameItem(slotA, itemA, slotB, itemB);
        }

        if (dataA.TryGetRecipeResult(dataB, out ItemData resultFromA))
        {
            LogDebug($"Receta encontrada desde {dataA.id} + {dataB.id} => {resultFromA.id}.");
            ApplyCraftResult(slotA, itemA, slotB, itemB, resultFromA);
            return true;
        }

        if (dataB.TryGetRecipeResult(dataA, out ItemData resultFromB))
        {
            LogDebug($"Receta encontrada desde {dataB.id} + {dataA.id} => {resultFromB.id}.");
            ApplyCraftResult(slotA, itemA, slotB, itemB, resultFromB);
            return true;
        }

        LogDebug($"No existe receta entre {dataA.id} y {dataB.id}.");
        return false;
    }

    private bool TryUpgradeSameItem(
        AnvilSlotUI slotA,
        DraggedItemVisual itemA,
        AnvilSlotUI slotB,
        DraggedItemVisual itemB
    )
    {
        ItemData dataA = itemA.ItemInstance.data;

        if (dataA == null)
        {
            LogDebug("TryUpgradeSameItem cancelado: dataA es null.");
            return false;
        }

        ItemRarity highestRarity = GetHighestRarity(itemA.ItemInstance.rarity, itemB.ItemInstance.rarity);
        ItemRarity upgradedRarity = GetNextRarity(highestRarity);

        if (upgradedRarity == highestRarity)
        {
            LogDebug($"No se puede subir rareza porque ya esta en el maximo: {highestRarity}.");
            return false;
        }

        LogDebug(
            $"Upgrade de item igual {dataA.id}: rarityA={itemA.ItemInstance.rarity}, " +
            $"rarityB={itemB.ItemInstance.rarity}, highest={highestRarity}, result={upgradedRarity}."
        );
        ApplyCraftResult(slotA, itemA, slotB, itemB, dataA, upgradedRarity);
        return true;
    }

    private void ApplyCraftResult(
        AnvilSlotUI slotA,
        DraggedItemVisual itemA,
        AnvilSlotUI slotB,
        DraggedItemVisual itemB,
        ItemData resultData,
        ItemRarity? overrideRarity = null
    )
    {
        if (resultData == null)
        {
            LogDebug("ApplyCraftResult cancelado: resultData es null.");
            return;
        }

        CrafterSide preservedSide = itemA.ItemInstance.crafterSide;
        ItemRarity resultRarity = overrideRarity ?? resultData.rarity;

        LogDebug(
            $"Aplicando resultado en slot ({slotA.Row}, {slotA.Column}): " +
            $"item={resultData.id}, rarity={resultRarity}, sidePreservado={preservedSide}. " +
            $"Se elimina slot ({slotB.Row}, {slotB.Column})."
        );

        List<PendingAnvilEffect> pendingEffects = ConsumePendingEffects();
        AnvilCraftOutcome outcome = new AnvilCraftOutcome(resultData, resultRarity);

        slotB.ClearItem();
        Destroy(itemB.gameObject);

        if (outcome.shouldDestroyResult)
        {
            slotA.ClearItem();
            Destroy(itemA.gameObject);
            SpawnRandomPendingEffect();
            return;
        }

        itemA.ReplaceItemData(outcome.resultData, outcome.resultRarity, preservedSide);
        IsResolvingCraftSequence = true;
        StartCoroutine(ResolveCraftSequence(itemA, pendingEffects, outcome, slotA, slotB, !overrideRarity.HasValue));
    }

    private IEnumerator ResolveCraftSequence(
        DraggedItemVisual craftedItem,
        List<PendingAnvilEffect> pendingEffects,
        AnvilCraftOutcome outcome,
        AnvilSlotUI resultSlot,
        AnvilSlotUI consumedSlot,
        bool isRecipe
    )
    {
        if (craftedItem != null)
        {
            yield return craftedItem.PlayImpactScaleAnimationAndWait();
        }

        ClearBurnedItems();
        if (isRecipe)
        {
            float enchantmentDuration = ApplyCraftEnchantments();
            if (enchantmentDuration > 0f)
                yield return new WaitForSecondsRealtime(enchantmentDuration);
        }
        float affectedItemsAnimationDuration = ApplyPendingEffects(pendingEffects, outcome, resultSlot, consumedSlot);

        if (affectedItemsAnimationDuration > 0f)
            yield return new WaitForSeconds(affectedItemsAnimationDuration);

        AnvilEffectVisual spawnedEffect = SpawnRandomPendingEffect();
        if (spawnedEffect != null)
        {
            spawnedEffect.PlayImpactScaleAnimation();
            yield return new WaitForSeconds(spawnedEffect.ImpactScaleAnimationDuration);
        }

        IsResolvingCraftSequence = false;
    }

    private float ApplyCraftEnchantments()
    {
        GetGridSize(out int rows, out int columns);
        Dictionary<DraggedItemVisual, float> bonuses = new();
        foreach (AnvilSlotUI source in slots)
        {
            var enchantments = source != null ? source.CurrentItem?.ItemInstance?.enchantments : null;
            if (enchantments == null) continue;
            foreach (EnchantmentInstance enchantment in enchantments)
            {
                if (enchantment?.data == null || enchantment.data.effectKind != EnchantmentData.EffectKind.DamageOnCraft)
                    continue;
                foreach (AnvilSlotUI target in slots)
                {
                    DraggedItemVisual item = target != null ? target.CurrentItem : null;
                    if (item?.ItemInstance == null || !GridPlacementRuleUtility.IsSlotAffectedByRule(
                        enchantment.targetRule, source.Row, source.Column, target.Row, target.Column, rows, columns))
                        continue;
                    float amount = enchantment.data.damageIncreaseAmount;
                    if (!item.ItemInstance.IncreaseDamage(amount)) continue;
                    bonuses.TryGetValue(item, out float previous);
                    bonuses[item] = previous + amount;
                }
            }
        }
        foreach (var bonus in bonuses)
            bonus.Key.PlayDamageIncreaseAnimation(bonus.Value);
        return bonuses.Count > 0 ? 0.9f : 0f;
    }

    private void ClearBurnedItems()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            DraggedItemVisual item = slots[i] != null ? slots[i].CurrentItem : null;
            if (item != null && item.IsBurned)
                item.SetBurned(false);
        }
    }

    private List<PendingAnvilEffect> ConsumePendingEffects()
    {
        List<PendingAnvilEffect> effects = new();
        ClearEffectTargetHighlights();

        for (int i = 0; i < slots.Count; i++)
        {
            AnvilSlotUI slot = slots[i];
            if (slot == null || !slot.HasEffect || slot.CurrentEffect == null)
                continue;

            if (slot.CurrentEffect.EffectData != null)
            {
                effects.Add(new PendingAnvilEffect
                {
                    data = slot.CurrentEffect.EffectData,
                    slot = slot,
                    targetRule = slot.CurrentEffect.TargetRule
                });
            }

            slot.ClearEffect();
        }

        return effects;
    }

    private float ApplyPendingEffects(
        List<PendingAnvilEffect> effects,
        AnvilCraftOutcome outcome,
        AnvilSlotUI resultSlot,
        AnvilSlotUI consumedSlot
    )
    {
        if (effects == null || outcome == null || resultSlot == null)
            return 0f;

        GetGridSize(out int rowCount, out int columnCount);
        float longestAnimationDuration = 0f;

        for (int i = 0; i < effects.Count; i++)
        {
            PendingAnvilEffect pendingEffect = effects[i];
            if (pendingEffect == null || pendingEffect.data == null || pendingEffect.slot == null)
                continue;

            List<AnvilSlotUI> affectedSlots = GetAffectedItemSlots(pendingEffect, rowCount, columnCount);
            bool affectsCraft = affectedSlots.Count > 0;

            if (!affectsCraft)
            {
                LogDebug(
                    $"Efecto pendiente {pendingEffect.data.id} no encontro items en su area. " +
                    $"area={pendingEffect.targetRule}, effectSlot=({pendingEffect.slot.Row},{pendingEffect.slot.Column}), " +
                    $"resultSlot=({resultSlot.Row},{resultSlot.Column})."
                );
                continue;
            }

            LogDebug(
                $"Activando efecto pendiente: {pendingEffect.data.id} " +
                $"({pendingEffect.data.kind}/{pendingEffect.data.GetActionDescription()}) " +
                $"con area {pendingEffect.targetRule}. Items afectados={affectedSlots.Count}."
            );

            for (int j = 0; j < affectedSlots.Count; j++)
            {
                float animationDuration = ApplyPendingEffectToSlot(pendingEffect, outcome, resultSlot, consumedSlot, affectedSlots[j]);
                longestAnimationDuration = Mathf.Max(longestAnimationDuration, animationDuration);
            }
        }

        return longestAnimationDuration;
    }

    private List<AnvilSlotUI> GetAffectedItemSlots(
        PendingAnvilEffect pendingEffect,
        int rowCount,
        int columnCount
    )
    {
        List<AnvilSlotUI> affectedSlots = new();

        if (pendingEffect == null || pendingEffect.slot == null)
            return affectedSlots;

        for (int i = 0; i < slots.Count; i++)
        {
            AnvilSlotUI targetSlot = slots[i];
            if (targetSlot == null || !targetSlot.HasItem)
                continue;

            bool isAffected = GridPlacementRuleUtility.IsSlotAffectedByRule(
                pendingEffect.targetRule,
                pendingEffect.slot.Row,
                pendingEffect.slot.Column,
                targetSlot.Row,
                targetSlot.Column,
                rowCount,
                columnCount
            );

            if (isAffected)
                affectedSlots.Add(targetSlot);
        }

        return affectedSlots;
    }

    private float ApplyPendingEffectToSlot(
        PendingAnvilEffect pendingEffect,
        AnvilCraftOutcome outcome,
        AnvilSlotUI resultSlot,
        AnvilSlotUI consumedSlot,
        AnvilSlotUI targetSlot
    )
    {
        if (pendingEffect == null || pendingEffect.data == null || targetSlot == null || !targetSlot.HasItem)
            return 0f;

        DraggedItemVisual targetItem = targetSlot.CurrentItem;
        if (targetItem == null)
            return 0f;

        if (pendingEffect.data.kind == AnvilEffectKind.Curse && targetItem.ConsumeProtection())
        {
            LogDebug($"Proteccion consumida: {DescribeItem(targetItem)} evita maldicion {pendingEffect.data.id}.");
            targetItem.PlayImpactScaleAnimation();
            return targetItem.ImpactScaleAnimationDuration;
        }

        AnvilEffectContext context = new AnvilEffectContext(
            pendingEffect.data,
            outcome,
            pendingEffect.slot,
            resultSlot,
            consumedSlot,
            targetSlot,
            targetItem,
            pendingEffect.targetRule
        );

        pendingEffect.data.ApplyTo(context);

        if (context.shouldDestroyTarget)
        {
            targetSlot.ClearItem();
            targetItem.DestroyAfterImpactScaleAnimation();
            return targetItem.ImpactScaleAnimationDuration;
        }

        targetItem.PlayImpactScaleAnimation();
        float longestAnimationDuration = targetItem.ImpactScaleAnimationDuration;

        if (context.shouldDuplicateTarget)
            longestAnimationDuration = Mathf.Max(longestAnimationDuration, TryDuplicateItem(targetItem));

        return longestAnimationDuration;
    }

    private void TryDuplicateResult(DraggedItemVisual sourceItem, AnvilCraftOutcome outcome, CrafterSide preservedSide)
    {
        if (sourceItem == null || sourceItem.ItemInstance == null || outcome == null || !outcome.shouldDuplicateResult)
            return;

        AnvilSlotUI duplicateSlot = GetRandomFreeSlot();
        if (duplicateSlot == null)
        {
            LogDebug("No hay espacio libre para duplicar el resultado.");
            return;
        }

        GameObject duplicateObject = Instantiate(sourceItem.gameObject);
        DraggedItemVisual duplicateVisual = duplicateObject.GetComponent<DraggedItemVisual>();

        if (duplicateVisual == null)
        {
            Destroy(duplicateObject);
            return;
        }

        ItemInstance duplicateInstance = new ItemInstance(outcome.resultData, preservedSide, sourceItem.ItemInstance.placementRule);
        duplicateInstance.rarity = outcome.resultRarity;
        duplicateInstance.isBurned = sourceItem.ItemInstance.isBurned;
        duplicateVisual.Initialize(duplicateInstance, null);
        duplicateVisual.SnapToAnvilSlot(duplicateSlot, false);
        duplicateVisual.PlayImpactScaleAnimation();
    }

    private float TryDuplicateItem(DraggedItemVisual sourceItem)
    {
        if (sourceItem == null || sourceItem.ItemInstance == null)
            return 0f;

        AnvilSlotUI duplicateSlot = GetRandomFreeSlot();
        if (duplicateSlot == null)
        {
            LogDebug("No hay espacio libre para duplicar el item afectado.");
            return 0f;
        }

        GameObject duplicateObject = Instantiate(sourceItem.gameObject);
        DraggedItemVisual duplicateVisual = duplicateObject.GetComponent<DraggedItemVisual>();

        if (duplicateVisual == null)
        {
            Destroy(duplicateObject);
            return 0f;
        }

        ItemInstance sourceInstance = sourceItem.ItemInstance;
        ItemInstance duplicateInstance = new ItemInstance(
            sourceInstance.data,
            sourceInstance.crafterSide,
            sourceInstance.placementRule
        );
        duplicateInstance.rarity = sourceInstance.rarity;
        duplicateInstance.isProtected = sourceInstance.isProtected;
        duplicateInstance.isBurned = sourceInstance.isBurned;
        duplicateInstance.permanentFlatDamageBonus = sourceInstance.permanentFlatDamageBonus;

        duplicateVisual.Initialize(duplicateInstance, null);
        duplicateVisual.SnapToAnvilSlot(duplicateSlot, false);
        duplicateVisual.PlayImpactScaleAnimation();
        return duplicateVisual.ImpactScaleAnimationDuration;
    }

    private AnvilEffectVisual SpawnRandomPendingEffect()
    {
        if (!enableRandomAnvilEffects)
        {
            LogDebug("La generación aleatoria de bendiciones y maldiciones está desactivada.");
            return null;
        }

        AnvilSlotUI targetSlot = GetRandomFreeSlot();
        if (targetSlot == null)
        {
            LogDebug("No hay espacio libre para crear bendicion/maldicion.");
            return null;
        }

        AnvilEffectData effect = RollRandomEffect();
        if (effect == null)
        {
            LogDebug("No hay bendiciones/maldiciones configuradas para crear.");
            return null;
        }

        GameObject effectObject = new GameObject(effect.effectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AnvilEffectVisual));
        effectObject.transform.SetParent(targetSlot.transform, false);

        GridPlacementRule targetRule = RollRandomEffectTargetRule();
        AnvilEffectVisual visual = effectObject.GetComponent<AnvilEffectVisual>();
        visual.Initialize(effect, targetRule);
        targetSlot.SetEffect(visual);
        RefreshEffectTargetHighlights();

        LogDebug($"Se creo {(effect.kind == AnvilEffectKind.Blessing ? "bendicion" : "maldicion")} {effect.id} en slot ({targetSlot.Row}, {targetSlot.Column}).");
        return visual;
    }

    private AnvilEffectData RollRandomEffect()
    {
        EnsureEffectsLoaded();

        bool rollBlessing = Random.value < 0.5f;
        List<AnvilEffectData> pool = rollBlessing ? blessings : curses;

        if (pool.Count == 0)
            pool = rollBlessing ? curses : blessings;

        if (pool.Count == 0)
            return CreateFallbackEffect(rollBlessing ? AnvilEffectKind.Blessing : AnvilEffectKind.Curse);

        return GetWeightedRandom(pool);
    }

    private void EnsureEffectsLoaded()
    {
        if (effectsLoaded)
            return;

        effectsLoaded = true;

        AnvilEffectData[] loadedEffects = Resources.LoadAll<AnvilEffectData>("ScriptableObjects/effects");
        for (int i = 0; i < loadedEffects.Length; i++)
        {
            AnvilEffectData effect = loadedEffects[i];
            if (effect == null)
                continue;

            List<AnvilEffectData> targetList = effect.kind == AnvilEffectKind.Blessing ? blessings : curses;
            if (!targetList.Contains(effect))
                targetList.Add(effect);
        }
    }

    private AnvilEffectData GetWeightedRandom(List<AnvilEffectData> pool)
    {
        int totalWeight = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] == null)
                continue;

            totalWeight += Mathf.Max(0, pool[i].spawnWeight);
        }

        if (totalWeight <= 0)
            return pool[Random.Range(0, pool.Count)];

        int roll = Random.Range(0, totalWeight);
        int current = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] == null)
                continue;

            current += Mathf.Max(0, pool[i].spawnWeight);
            if (roll < current)
                return pool[i];
        }

        return pool[pool.Count - 1];
    }

    private AnvilEffectData CreateFallbackEffect(AnvilEffectKind kind)
    {
        AnvilEffectData effect = ScriptableObject.CreateInstance<AnvilEffectData>();
        effect.kind = kind;
        effect.spawnWeight = 1;
        effect.raritySteps = 1;
        effect.backgroundColor = kind == AnvilEffectKind.Blessing
            ? new Color(0.35f, 0.9f, 0.55f, 0.85f)
            : new Color(0.75f, 0.2f, 0.85f, 0.85f);

        if (kind == AnvilEffectKind.Blessing)
        {
            effect.id = "fallback_plus_rarity";
            effect.effectName = "Bendicion de rareza";
            effect.effectType = AnvilEffectType.IncreaseResultRarity;
            effect.icon = Resources.Load<Sprite>("Images/blessings/plus-rarity");
            return effect;
        }

        effect.id = "fallback_minus_rarity";
        effect.effectName = "Maldicion de rareza";
        effect.effectType = AnvilEffectType.DecreaseResultRarity;
        effect.icon = Resources.Load<Sprite>("Images/curses/menus-rarity");
        return effect;
    }

    private AnvilSlotUI GetRandomFreeSlot()
    {
        List<AnvilSlotUI> freeSlots = new();

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && !slots[i].IsOccupied)
                freeSlots.Add(slots[i]);
        }

        if (freeSlots.Count == 0)
            return null;

        return freeSlots[Random.Range(0, freeSlots.Count)];
    }

    private void RefreshEffectTargetHighlights()
    {
        ClearEffectTargetHighlights();
        GetGridSize(out int rowCount, out int columnCount);

        for (int i = 0; i < slots.Count; i++)
        {
            AnvilSlotUI effectSlot = slots[i];
            if (effectSlot == null || !effectSlot.HasEffect || effectSlot.CurrentEffect == null)
                continue;

            AnvilEffectData effect = effectSlot.CurrentEffect.EffectData;
            if (effect == null)
                continue;

            for (int j = 0; j < slots.Count; j++)
            {
                AnvilSlotUI targetSlot = slots[j];
                if (targetSlot == null || targetSlot == effectSlot)
                    continue;

                bool isAffected = GridPlacementRuleUtility.IsSlotAffectedByRule(
                    effectSlot.CurrentEffect.TargetRule,
                    effectSlot.Row,
                    effectSlot.Column,
                    targetSlot.Row,
                    targetSlot.Column,
                    rowCount,
                    columnCount
                );

                if (isAffected)
                    targetSlot.ShowEffectTargetHighlight(effect.kind);
            }
        }
    }

    private GridPlacementRule RollRandomEffectTargetRule()
    {
        IReadOnlyList<GridPlacementRule> targetRules = GridPlacementRuleUtility.GetEffectTargetRules();
        if (targetRules == null || targetRules.Count == 0)
            return GridPlacementRule.Adjacent;

        return targetRules[Random.Range(0, targetRules.Count)];
    }

    private void ClearEffectTargetHighlights()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
                slots[i].ClearEffectTargetHighlight();
        }
    }

    private ItemRarity GetNextRarity(ItemRarity currentRarity)
    {
        if (currentRarity >= ItemRarity.Legendary)
            return ItemRarity.Legendary;

        return currentRarity + 1;
    }

    private ItemRarity GetHighestRarity(ItemRarity rarityA, ItemRarity rarityB)
    {
        return rarityA >= rarityB ? rarityA : rarityB;
    }

    private void GetGridSize(out int rowCount, out int columnCount)
    {
        rowCount = 0;
        columnCount = 0;

        if (slots.Count == 0)
            return;

        int maxRow = -1;
        int maxColumn = -1;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                continue;

            if (slots[i].Row > maxRow)
                maxRow = slots[i].Row;

            if (slots[i].Column > maxColumn)
                maxColumn = slots[i].Column;
        }

        rowCount = maxRow + 1;
        columnCount = maxColumn + 1;
    }

    private void LogDebug(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.LogWarning($"[AnvilCraftManager] {message}", this);
    }

    private string DescribeItem(DraggedItemVisual item)
    {
        if (item == null || item.ItemInstance == null || item.ItemInstance.data == null)
            return "<null>";

        return $"{item.ItemInstance.data.id} rarity={item.ItemInstance.rarity} side={item.ItemInstance.crafterSide}";
    }
}
