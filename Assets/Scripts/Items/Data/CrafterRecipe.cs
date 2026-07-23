using System;
using UnityEngine;

[Serializable]
public class CrafterRecipe
{
    [Header("Input")]
    public ItemData otherItem;

    [Header("Result")]
    public ItemData resultItem;

    [Header("Rules")]
    public bool requiresSameRarity = true;
}