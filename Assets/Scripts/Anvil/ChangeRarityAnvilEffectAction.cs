using UnityEngine;

[CreateAssetMenu(fileName = "ChangeRarityAction", menuName = "Game/Anvil/Actions/Change Rarity")]
public class ChangeRarityAnvilEffectAction : AnvilEffectAction
{
    [Tooltip("Usa positivo para subir rareza y negativo para bajarla.")]
    [SerializeField] private int raritySteps = 1;

    public override void Apply(AnvilEffectContext context)
    {
        if (context == null || context.targetItem == null || context.targetItem.ItemInstance == null)
            return;

        int min = (int)ItemRarity.Common;
        int max = (int)ItemRarity.Legendary;
        ItemInstance instance = context.targetItem.ItemInstance;
        int next = Mathf.Clamp((int)instance.rarity + raritySteps, min, max);

        context.targetItem.ReplaceItemData(instance.data, (ItemRarity)next, instance.crafterSide);
    }
}
