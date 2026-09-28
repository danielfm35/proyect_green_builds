using System;
using UnityEngine;

[Serializable]
public sealed class BossAttackStep
{
    [Tooltip("Ability used by this step. It can be left empty while abilities are being implemented.")]
    public ScriptableObject ability;

    [Min(0f)]
    [Tooltip("Delay before advancing to the next step.")]
    public float delayAfterAttack = 1f;
}
