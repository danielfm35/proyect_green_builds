using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBoss", menuName = "Game/Battle/Boss Data")]
public sealed class BossData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string bossId;
    [SerializeField] private string displayName = "New Boss";
    [TextArea, SerializeField] private string description;
    [SerializeField] private Sprite portrait;
    [Tooltip("Llena el marco con el retrato y muestra su parte superior.")]
    [SerializeField] private bool cropPortraitToFrame;

    [Header("Combat")]
    [SerializeField, Min(0)] private int encounterOrder;
    [SerializeField] private bool isFinalBoss;
    [Min(1), SerializeField] private int maximumHealth = 100;
    [Min(0), SerializeField] private int attackDamage = 40;
    [Tooltip("La armadura reduce todo el daño. La resistencia magica reduce adicionalmente el daño Magic.")]
    [SerializeField] private DamageType attackDamageType = DamageType.Physical;

    [Header("Future combat configuration")]
    [Tooltip("Ability assets available to this boss.")]
    [SerializeField] private List<ScriptableObject> abilities = new();
    [Tooltip("Ordered attack sequence. Runtime behaviour can consume this list later.")]
    [SerializeField] private List<BossAttackStep> attackPattern = new();

    public string BossId => bossId;
    public int EncounterOrder => encounterOrder;
    public bool IsFinalBoss => isFinalBoss;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Portrait => portrait;
    public bool CropPortraitToFrame => cropPortraitToFrame;
    public int MaximumHealth => Mathf.Max(1, maximumHealth);
    public int AttackDamage => Mathf.Max(0, attackDamage);
    public DamageType AttackDamageType => attackDamageType;
    public IReadOnlyList<ScriptableObject> Abilities => abilities;
    public IReadOnlyList<BossAttackStep> AttackPattern => attackPattern;
}
