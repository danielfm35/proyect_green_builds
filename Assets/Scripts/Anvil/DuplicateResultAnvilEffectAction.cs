using UnityEngine;

[CreateAssetMenu(fileName = "DuplicateResultAction", menuName = "Game/Anvil/Actions/Duplicate Result")]
public class DuplicateResultAnvilEffectAction : AnvilEffectAction
{
    public override void Apply(AnvilEffectContext context)
    {
        if (context == null || context.targetItem == null)
            return;

        context.shouldDuplicateTarget = true;
    }
}
