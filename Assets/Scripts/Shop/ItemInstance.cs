using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemInstance
{
    public ItemData data;
    public ItemRarity rarity;
    public CrafterSide crafterSide;
    public GridPlacementRule placementRule;
    public bool isProtected;
    public bool isBurned;
    public float permanentFlatDamageBonus;
    public List<EnchantmentInstance> enchantments = new();

    public int EnchantmentCount => enchantments != null ? enchantments.Count : 0;
    public int MaxEnchantments => data != null ? data.MaxEnchantments : 0;

    public List<ItemStat> GetEffectiveStats()
    {
        List<ItemStat> result = new();
        if (data == null || data.stats == null)
            return result;

        // Asset values describe the item's original rarity. Each upgrade doubles them.
        int upgrades = Mathf.Max(0, (int)rarity - (int)data.rarity);
        float multiplier = Mathf.Pow(2f, upgrades);
        // Split the fixed bonus so an item with multiple damage types gains the same total.
        int damageStatCount = data.stats.FindAll(IsPositiveNumericDamage).Count;
        float bonusPerStat = damageStatCount > 0 ? permanentFlatDamageBonus / damageStatCount : 0f;
        foreach (ItemStat stat in data.stats)
        {
            if (stat == null)
                continue;

            result.Add(new ItemStat
            {
                statType = stat.statType,
                valueType = stat.valueType,
                value = stat.value * multiplier + (IsPositiveNumericDamage(stat) ? bonusPerStat : 0f)
            });
        }
        return result;
    }

    public ItemInstance(ItemData itemData, CrafterSide side, GridPlacementRule rule = GridPlacementRule.None)
    {
        data = itemData;
        rarity = itemData != null ? itemData.rarity : ItemRarity.Common;
        crafterSide = side;
        placementRule = rule;
        isProtected = false;
        isBurned = false;
    }

    public static bool IsDamageStat(StatType type) => type == StatType.SlashingDamage ||
        type == StatType.PiercingDamage || type == StatType.BludgeoningDamage || type == StatType.CounterattackDamage;

    private static bool IsPositiveNumericDamage(ItemStat stat) => stat != null &&
        IsDamageStat(stat.statType) && stat.valueType == ItemStatValueType.Numeric && stat.value > 0f;

    public bool IncreaseDamage(float amount)
    {
        if (amount <= 0f || data == null || data.stats == null ||
            !data.stats.Exists(IsPositiveNumericDamage))
            return false;

        permanentFlatDamageBonus += amount;
        return true;
    }

    public bool TryAddEnchantment(EnchantmentInstance enchantment)
    {
        if (enchantment == null || enchantment.data == null || MaxEnchantments <= 0 || EnchantmentCount >= MaxEnchantments)
            return false;

        enchantments ??= new List<EnchantmentInstance>();
        enchantments.Add(enchantment);
        return true;
    }
}
