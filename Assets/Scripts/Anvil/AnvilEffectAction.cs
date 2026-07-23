using UnityEngine;

public abstract class AnvilEffectAction : ScriptableObject
{
    public abstract void Apply(AnvilEffectContext context);
}
