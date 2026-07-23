using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Items/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string itemName;

    [TextArea]
    public string description;

    [Header("Localization")]
    public LocalizedText localizedName = new();

    public LocalizedText localizedDescription = new();

    public Sprite icon;

    [Header("Classification")]
    public ItemType itemType;

    [Tooltip("Only used when Item Type is Weapon.")]
    public WeaponHandedness weaponHandedness;

    [Tooltip("Only used when Item Type is Armor.")]
    public ArmorCategory armorCategory;

    public ItemRarity rarity;

    [Header("Economy")]
    public int cost = 1;

    [Header("Stats")]
    public List<ItemStat> stats = new();

    [Header("Crafting")]
    public bool isCraftedOnly;
    public ItemData upgradedVersion;
    public List<CraftRecipe> recipes = new();

    public string GetDisplayName()
    {
        if (localizedName == null)
            return string.IsNullOrWhiteSpace(itemName) ? id : itemName;

        string fallback = string.IsNullOrWhiteSpace(itemName) ? id : itemName;
        return localizedName.GetOrFallback(fallback);
    }

    public string GetDescription()
    {
        if (localizedDescription == null)
            return description;

        return localizedDescription.GetOrFallback(description);
    }

    public bool TryGetRecipeResult(ItemData otherItem, out ItemData result)
    {
        result = null;

        if (otherItem == null)
            return false;

        for (int i = 0; i < recipes.Count; i++)
        {
            CraftRecipe recipe = recipes[i];

            if (recipe == null || recipe.otherItem == null || recipe.resultItem == null)
                continue;

            if (recipe.otherItem.id == otherItem.id)
            {
                result = recipe.resultItem;
                return true;
            }
        }

        return false;
    }
}
