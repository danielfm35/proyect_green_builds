using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Game/Abilities/Ability")]
public sealed class AbilityData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string abilityName;

    [TextArea]
    public string description;

    public Sprite icon;

    [Header("Activation")]
    [Range(1, 20)]
    [Tooltip("Minimum result required on a d20 roll to activate this ability.")]
    public int minimumD20Roll = 10;

    [Header("Combat Effect")]
    [Min(0)]
    [Tooltip("Damage dealt to the enemy after it attacks while this ability is active.")]
    public int damageAfterEnemyAttack;

    [Min(1)]
    [Tooltip("Number of enemy attacks for which the ability remains active.")]
    public int durationInEnemyAttacks = 1;

    public bool IsActivatedBy(int roll)
    {
        return Mathf.Clamp(roll, 1, 20) >= minimumD20Roll;
    }

    public bool RollForActivation(out int roll)
    {
        roll = Random.Range(1, 21);
        return IsActivatedBy(roll);
    }

    private void OnValidate()
    {
        minimumD20Roll = Mathf.Clamp(minimumD20Roll, 1, 20);
        damageAfterEnemyAttack = Mathf.Max(0, damageAfterEnemyAttack);
        durationInEnemyAttacks = Mathf.Max(1, durationInEnemyAttacks);
    }
}
