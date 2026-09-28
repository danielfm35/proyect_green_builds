using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayer", menuName = "Game/Battle/Player Data")]
public sealed class PlayerData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string playerId;
    [SerializeField] private string displayName = "Player";
    [SerializeField] private Sprite portrait;

    [Header("Health")]
    [Min(1), SerializeField] private int maximumHealth = 300;

    [Header("Battle multipliers")]
    [Min(0f), SerializeField] private float healthMultiplier = 1f;
    [Min(0f), SerializeField] private float shieldMultiplier = 1f;
    [Min(0f), SerializeField] private float damageMultiplier = 1f;
    [Min(0f), SerializeField] private float magicResistMultiplier = 1f;

    public string PlayerId => playerId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Portrait => portrait;
    public int MaximumHealth => Mathf.Max(1, maximumHealth);
    public float HealthMultiplier => Mathf.Max(0f, healthMultiplier);
    public float ShieldMultiplier => Mathf.Max(0f, shieldMultiplier);
    public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
    public float MagicResistMultiplier => Mathf.Max(0f, magicResistMultiplier);
}
