using UnityEngine;

public static class GameTextLocalizer
{
    private static GameLanguage currentLanguage = DetectInitialLanguage();

    public static GameLanguage CurrentLanguage => currentLanguage;

    public static void SetLanguage(GameLanguage language)
    {
        currentLanguage = language;
    }

    public static string LocalizeRarity(ItemRarity rarity)
    {
        if (currentLanguage == GameLanguage.Spanish)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return "COMUN";
                case ItemRarity.Uncommon: return "POCO COMUN";
                case ItemRarity.Rare: return "RARO";
                case ItemRarity.Epic: return "EPICO";
                case ItemRarity.Legendary: return "LEGENDARIO";
            }
        }

        switch (rarity)
        {
            case ItemRarity.Common: return "COMMON";
            case ItemRarity.Uncommon: return "UNCOMMON";
            case ItemRarity.Rare: return "RARE";
            case ItemRarity.Epic: return "EPIC";
            case ItemRarity.Legendary: return "LEGENDARY";
            default: return rarity.ToString().ToUpperInvariant();
        }
    }

    public static string LocalizeItemType(ItemData itemData)
    {
        if (itemData == null)
            return string.Empty;

        if (currentLanguage == GameLanguage.Spanish)
        {
            switch (itemData.itemType)
            {
                case ItemType.Weapon: return "ARMA";
                case ItemType.Armor: return LocalizeArmorCategory(itemData.armorCategory);
                case ItemType.Material: return "MATERIAL";
                case ItemType.Shield: return "ESCUDO";
            }
        }

        switch (itemData.itemType)
        {
            case ItemType.Weapon: return "WEAPON";
            case ItemType.Armor: return LocalizeArmorCategory(itemData.armorCategory);
            case ItemType.Material: return "MATERIAL";
            case ItemType.Shield: return "SHIELD";
            default: return itemData.itemType.ToString().ToUpperInvariant();
        }
    }

    public static string LocalizeNoDescription()
    {
        return currentLanguage == GameLanguage.Spanish
            ? "Sin descripcion."
            : "No description.";
    }

    public static string LocalizeAnvilEffectKind(AnvilEffectKind kind)
    {
        if (currentLanguage == GameLanguage.Spanish)
            return kind == AnvilEffectKind.Blessing ? "BENDICION" : "MALDICION";

        return kind == AnvilEffectKind.Blessing ? "BLESSING" : "CURSE";
    }

    public static string LocalizeAnvilEffectName(AnvilEffectData effectData)
    {
        if (effectData == null)
            return string.Empty;

        string localizedName = LocalizeAnvilEffectTypeName(effectData.effectType);
        if (!string.IsNullOrWhiteSpace(localizedName))
            return localizedName;

        return string.IsNullOrWhiteSpace(effectData.effectName) ? effectData.id : effectData.effectName;
    }

    public static string LocalizeAnvilEffectDescription(AnvilEffectData effectData)
    {
        if (effectData == null)
            return LocalizeNoDescription();

        string localizedDescription = LocalizeAnvilEffectTypeDescription(effectData.effectType, effectData.raritySteps);
        if (!string.IsNullOrWhiteSpace(localizedDescription))
            return localizedDescription;

        return string.IsNullOrWhiteSpace(effectData.description)
            ? LocalizeNoDescription()
            : effectData.description;
    }

    public static string LocalizePlacementRuleDescription(GridPlacementRule rule)
    {
        if (currentLanguage == GameLanguage.Spanish)
        {
            switch (rule)
            {
                case GridPlacementRule.Center:
                    return "Centro: solo puede ocupar espacios centrales del yunque.";
                case GridPlacementRule.Corners:
                    return "Esquinas: solo puede ocupar espacios de las esquinas del yunque.";
                case GridPlacementRule.Up:
                    return "Arriba: solo puede ocupar espacios de arriba del yunque.";
                case GridPlacementRule.Down:
                    return "Abajo: solo puede ocupar espacios de abajo del yunque.";
                case GridPlacementRule.Left:
                    return "Izquierda: solo puede ocupar espacios de la izquierda del yunque.";
                case GridPlacementRule.Right:
                    return "Derecha: solo puede ocupar espacios de la derecha del yunque.";
                case GridPlacementRule.LeftRight:
                    return "Izquierda/derecha: solo puede ocupar espacios de la izquierda y de la derecha del yunque.";
                case GridPlacementRule.UpDown:
                    return "Arriba/abajo: solo puede ocupar espacios de arriba y de abajo del yunque.";
                case GridPlacementRule.Adjacent:
                    return "Adyacente: afecta espacios adyacentes en el yunque.";
                default:
                    return string.Empty;
            }
        }

        switch (rule)
        {
            case GridPlacementRule.Center:
                return "Center: can only occupy center anvil spaces.";
            case GridPlacementRule.Corners:
                return "Corners: can only occupy corner anvil spaces.";
            case GridPlacementRule.Up:
                return "Top: can only occupy top anvil spaces.";
            case GridPlacementRule.Down:
                return "Bottom: can only occupy bottom anvil spaces.";
            case GridPlacementRule.Left:
                return "Left: can only occupy left anvil spaces.";
            case GridPlacementRule.Right:
                return "Right: can only occupy right anvil spaces.";
            case GridPlacementRule.LeftRight:
                return "Left/right: can only occupy left and right anvil spaces.";
            case GridPlacementRule.UpDown:
                return "Top/bottom: can only occupy top and bottom anvil spaces.";
            case GridPlacementRule.Adjacent:
                return "Adjacent: affects adjacent anvil spaces.";
            default:
                return string.Empty;
        }
    }

    public static string LocalizeStatType(StatType statType)
    {
        if (currentLanguage == GameLanguage.Spanish)
        {
            switch (statType)
            {
                case StatType.Armor: return "armadura";
                case StatType.HP: return "vida";
                case StatType.CritChance: return "prob. critico";
                case StatType.MagicResist: return "resistencia magica";
                case StatType.CriticalDamage: return "dano critico";
                case StatType.PiercingDamage: return "dano perforante";
                case StatType.SlashingDamage: return "dano cortante";
                case StatType.BludgeoningDamage: return "dano contundente";
                case StatType.Dodge: return "evasion";
                case StatType.Lifesteal: return "robo de vida";
                case StatType.ArmorPenetration: return "penetracion armadura";
                case StatType.CounterattackDamage: return "dano contraataque";
                case StatType.StunChance: return "prob. aturdimiento";
            }
        }

        switch (statType)
        {
            case StatType.Armor: return "armor";
            case StatType.HP: return "hp";
            case StatType.CritChance: return "crit chance";
            case StatType.MagicResist: return "magic resist";
            case StatType.CriticalDamage: return "critical damage";
            case StatType.PiercingDamage: return "piercing damage";
            case StatType.SlashingDamage: return "slashing damage";
            case StatType.BludgeoningDamage: return "bludgeoning damage";
            case StatType.Dodge: return "dodge";
            case StatType.Lifesteal: return "lifesteal";
            case StatType.ArmorPenetration: return "armor penetration";
            case StatType.CounterattackDamage: return "counterattack damage";
            case StatType.StunChance: return "stun chance";
            default: return statType.ToString();
        }
    }

    private static string LocalizeArmorCategory(ArmorCategory category)
    {
        if (currentLanguage == GameLanguage.Spanish)
        {
            switch (category)
            {
                case ArmorCategory.Armor: return "ARMADURA";
                case ArmorCategory.Boots: return "BOTAS";
                case ArmorCategory.Gloves: return "GUANTES";
                case ArmorCategory.Helmet: return "CASCO";
                case ArmorCategory.Shoulder: return "HOMBRERA";
            }
        }

        switch (category)
        {
            case ArmorCategory.Armor: return "ARMOR";
            case ArmorCategory.Boots: return "BOOTS";
            case ArmorCategory.Gloves: return "GLOVES";
            case ArmorCategory.Helmet: return "HELMET";
            case ArmorCategory.Shoulder: return "SHOULDER";
            default: return "ARMOR";
        }
    }

    private static string LocalizeAnvilEffectTypeName(AnvilEffectType effectType)
    {
        if (currentLanguage == GameLanguage.Spanish)
        {
            switch (effectType)
            {
                case AnvilEffectType.IncreaseResultRarity: return "Incrementar rareza";
                case AnvilEffectType.DecreaseResultRarity: return "Reducir rareza";
                case AnvilEffectType.DuplicateResult: return "Duplicar resultado";
                case AnvilEffectType.DestroyResult: return "Destruir resultado";
                case AnvilEffectType.Protect: return "Proteccion";
                case AnvilEffectType.Burn: return "Quemadura";
            }
        }

        switch (effectType)
        {
            case AnvilEffectType.IncreaseResultRarity: return "Increase Rarity";
            case AnvilEffectType.DecreaseResultRarity: return "Decrease Rarity";
            case AnvilEffectType.DuplicateResult: return "Duplicate Result";
            case AnvilEffectType.DestroyResult: return "Destroy Result";
            case AnvilEffectType.Protect: return "Protection";
            case AnvilEffectType.Burn: return "Burn";
            default: return string.Empty;
        }
    }

    private static string LocalizeAnvilEffectTypeDescription(AnvilEffectType effectType, int raritySteps)
    {
        int steps = Mathf.Max(1, raritySteps);

        if (currentLanguage == GameLanguage.Spanish)
        {
            switch (effectType)
            {
                case AnvilEffectType.IncreaseResultRarity:
                    return steps == 1
                        ? "Sube la rareza del item afectado en 1 nivel."
                        : $"Sube la rareza del item afectado en {steps} niveles.";
                case AnvilEffectType.DecreaseResultRarity:
                    return steps == 1
                        ? "Baja la rareza del item afectado en 1 nivel."
                        : $"Baja la rareza del item afectado en {steps} niveles.";
                case AnvilEffectType.DuplicateResult:
                    return "Duplica el item afectado.";
                case AnvilEffectType.DestroyResult:
                    return "Destruye el item afectado.";
                case AnvilEffectType.Protect:
                    return "Agrega una capa de proteccion. La siguiente maldicion destructiva consume la proteccion en vez de destruir el item.";
                case AnvilEffectType.Burn:
                    return "Quema el item afectado y evita que se pueda mover hasta que vuelva a ser crafteado.";
            }
        }

        switch (effectType)
        {
            case AnvilEffectType.IncreaseResultRarity:
                return steps == 1
                    ? "Raises the affected item's rarity by 1 level."
                    : $"Raises the affected item's rarity by {steps} levels.";
            case AnvilEffectType.DecreaseResultRarity:
                return steps == 1
                    ? "Lowers the affected item's rarity by 1 level."
                    : $"Lowers the affected item's rarity by {steps} levels.";
            case AnvilEffectType.DuplicateResult:
                return "Duplicates the affected item.";
            case AnvilEffectType.DestroyResult:
                return "Destroys the affected item.";
            case AnvilEffectType.Protect:
                return "Adds a protection layer. The next destructive curse consumes the protection instead of destroying the item.";
            case AnvilEffectType.Burn:
                return "Burns the affected item and prevents it from being moved until it is crafted again.";
            default:
                return string.Empty;
        }
    }

    private static GameLanguage DetectInitialLanguage()
    {
        return Application.systemLanguage == SystemLanguage.Spanish
            ? GameLanguage.Spanish
            : GameLanguage.English;
    }
}
