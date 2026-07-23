public class AnvilEffectContext
{
    public AnvilEffectData effectData;
    public AnvilCraftOutcome outcome;
    public AnvilSlotUI effectSlot;
    public AnvilSlotUI resultSlot;
    public AnvilSlotUI consumedSlot;
    public AnvilSlotUI targetSlot;
    public DraggedItemVisual targetItem;
    public GridPlacementRule targetRule;
    public bool shouldDuplicateTarget;
    public bool shouldDestroyTarget;

    public AnvilEffectContext(
        AnvilEffectData data,
        AnvilCraftOutcome craftOutcome,
        AnvilSlotUI sourceEffectSlot,
        AnvilSlotUI craftResultSlot,
        AnvilSlotUI craftConsumedSlot,
        AnvilSlotUI affectedSlot,
        DraggedItemVisual affectedItem,
        GridPlacementRule selectedTargetRule
    )
    {
        effectData = data;
        outcome = craftOutcome;
        effectSlot = sourceEffectSlot;
        resultSlot = craftResultSlot;
        consumedSlot = craftConsumedSlot;
        targetSlot = affectedSlot;
        targetItem = affectedItem;
        targetRule = selectedTargetRule;
    }
}
