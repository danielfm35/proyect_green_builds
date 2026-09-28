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

    [Tooltip("Tags used to group items with shared traits.")]
    public List<ItemGroup> itemGroups = new();

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
    [Range(0, 3)]
    [Tooltip("Craft level. Level 0 is uncrafted; levels 1-3 allow the same number of enchantments.")]
    public int level;

    public bool isCraftedOnly;
    public ItemData upgradedVersion;
    public List<CraftRecipe> recipes = new();

    [Header("Abilities")]
    [Tooltip("Abilities available to this item. An item can have at most one ability per level.")]
    public List<AbilityData> abilities = new();

    public int MaxEnchantments => Mathf.Clamp(level, 0, 3);
    public int MaxAbilities => Mathf.Max(0, level);

    public bool CanAddAbility(AbilityData ability)
    {
        return ability != null && !abilities.Contains(ability) && abilities.Count < MaxAbilities;
    }

    public bool TryAddAbility(AbilityData ability)
    {
        if (!CanAddAbility(ability))
            return false;

        abilities.Add(ability);
        return true;
    }

    public bool RemoveAbility(AbilityData ability)
    {
        return ability != null && abilities.Remove(ability);
    }

    private void OnValidate()
    {
        level = Mathf.Clamp(level, 0, 3);
        abilities ??= new List<AbilityData>();

        for (int i = abilities.Count - 1; i >= 0; i--)
        {
            if (abilities[i] == null || abilities.IndexOf(abilities[i]) != i)
                abilities.RemoveAt(i);
        }

        if (abilities.Count > MaxAbilities)
            abilities.RemoveRange(MaxAbilities, abilities.Count - MaxAbilities);
    }

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
