using UnityEngine;

[CreateAssetMenu(fileName = "DestroyResultAction", menuName = "Game/Anvil/Actions/Destroy Result")]
public class DestroyResultAnvilEffectAction : AnvilEffectAction
{
    public override void Apply(AnvilEffectContext context)
    {
        if (context == null || context.targetItem == null)
            return;

        context.shouldDestroyTarget = true;
    }
}
