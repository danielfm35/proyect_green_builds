public class AnvilCraftOutcome
{
    public ItemData resultData;
    public ItemRarity resultRarity;
    public bool shouldDuplicateResult;
    public bool shouldDestroyResult;

    public AnvilCraftOutcome(ItemData data, ItemRarity rarity)
    {
        resultData = data;
        resultRarity = rarity;
    }
}
