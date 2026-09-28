using UnityEngine;

[System.Serializable]
public sealed class EnchantmentInstance
{
    public EnchantmentData data;
    public GridPlacementRule targetRule;

    public EnchantmentInstance(EnchantmentData enchantmentData, GridPlacementRule rule)
    {
        data = enchantmentData;
        targetRule = rule;
    }
}

[CreateAssetMenu(fileName = "NewEnchantment", menuName = "Game/Enchantments/Enchantment")]
public sealed class EnchantmentData : ScriptableObject
{
    public enum EffectKind { BattleMultiplier, DamageOnCraft }

    public EffectKind effectKind;

    [Header("Craft Effect")]
    [Tooltip("Daño fijo permanente añadido a cada objeto por receta completada. No incluye subir rareza.")]
    [Min(0f)] public float damageIncreaseAmount = 10f;

    public string DisplayDescription => (description ?? string.Empty).Replace("{amount}", damageIncreaseAmount.ToString("0.##"));

    public string enchantmentName;

    public string id;

    [TextArea(3, 7)]
    public string description;

    public Sprite icon;

    [Header("Battle Effect")]
    public ItemGroup requiredGroup;

    [Min(0f)]
    public float damageMultiplierPerMatch = 0.5f;

    [Min(1)]
    public int maximumMatches = 3;
}
