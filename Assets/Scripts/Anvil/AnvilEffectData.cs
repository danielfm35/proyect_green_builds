using UnityEngine;

[CreateAssetMenu(fileName = "NewAnvilEffect", menuName = "Game/Anvil/Effect")]
public class AnvilEffectData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string effectName;

    [TextArea]
    public string description;

    [Header("Localization")]
    public LocalizedText localizedName = new();

    public LocalizedText localizedDescription = new();

    [Header("Visuals")]
    public Sprite icon;
    public Sprite background;
    public Color backgroundColor = Color.white;

    [Header("Effect")]
    public AnvilEffectKind kind;
    public AnvilEffectAction action;

    [Header("Legacy Fallback")]
    public AnvilEffectType effectType;

    [Tooltip("Solo se usa en efectos que suben o bajan rareza.")]
    [Min(1)] public int raritySteps = 1;

    [Tooltip("Probabilidad relativa de aparicion dentro de bendiciones o maldiciones.")]
    [Min(0)] public int spawnWeight = 1;

    public string GetActionDescription()
    {
        if (action != null)
            return action.name;

        return effectType.ToString();
    }

    public string GetDisplayName()
    {
        string fallback = GameTextLocalizer.LocalizeAnvilEffectName(this);

        if (localizedName == null)
            return fallback;

        return localizedName.GetOrFallback(fallback);
    }

    public string GetDescription()
    {
        string fallback = GameTextLocalizer.LocalizeAnvilEffectDescription(this);

        if (localizedDescription == null)
            return fallback;

        return localizedDescription.GetOrFallback(fallback);
    }

    public void ApplyTo(AnvilEffectContext context)
    {
        if (context == null || context.outcome == null)
            return;

        if (action != null)
        {
            action.Apply(context);
            return;
        }

        ApplyLegacyTo(context);
    }

    public void ApplyTo(AnvilCraftOutcome outcome)
    {
        ApplyLegacyTo(outcome);
    }

    private void ApplyLegacyTo(AnvilCraftOutcome outcome)
    {
        if (outcome == null)
            return;

        switch (effectType)
        {
            case AnvilEffectType.IncreaseResultRarity:
                outcome.resultRarity = ShiftRarity(outcome.resultRarity, raritySteps);
                break;
            case AnvilEffectType.DecreaseResultRarity:
                outcome.resultRarity = ShiftRarity(outcome.resultRarity, -raritySteps);
                break;
            case AnvilEffectType.DuplicateResult:
                outcome.shouldDuplicateResult = true;
                break;
            case AnvilEffectType.DestroyResult:
                outcome.shouldDestroyResult = true;
                break;
            case AnvilEffectType.Protect:
                break;
        }
    }

    private void ApplyLegacyTo(AnvilEffectContext context)
    {
        if (context == null || context.targetItem == null || context.targetItem.ItemInstance == null)
            return;

        ItemInstance instance = context.targetItem.ItemInstance;

        switch (effectType)
        {
            case AnvilEffectType.IncreaseResultRarity:
                context.targetItem.ReplaceItemData(instance.data, ShiftRarity(instance.rarity, raritySteps), instance.crafterSide);
                break;
            case AnvilEffectType.DecreaseResultRarity:
                context.targetItem.ReplaceItemData(instance.data, ShiftRarity(instance.rarity, -raritySteps), instance.crafterSide);
                break;
            case AnvilEffectType.DuplicateResult:
                context.shouldDuplicateTarget = true;
                break;
            case AnvilEffectType.DestroyResult:
                context.shouldDestroyTarget = true;
                break;
            case AnvilEffectType.Protect:
                context.targetItem.AddProtection();
                break;
            case AnvilEffectType.Burn:
                context.targetItem.SetBurned(true);
                break;
        }
    }

    private ItemRarity ShiftRarity(ItemRarity rarity, int steps)
    {
        int min = (int)ItemRarity.Common;
        int max = (int)ItemRarity.Legendary;
        int next = Mathf.Clamp((int)rarity + steps, min, max);
        return (ItemRarity)next;
    }
}
