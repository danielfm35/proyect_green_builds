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

    public ItemInstance(ItemData itemData, CrafterSide side, GridPlacementRule rule = GridPlacementRule.None)
    {
        data = itemData;
        rarity = itemData != null ? itemData.rarity : ItemRarity.Common;
        crafterSide = side;
        placementRule = rule;
        isProtected = false;
        isBurned = false;
    }
}
