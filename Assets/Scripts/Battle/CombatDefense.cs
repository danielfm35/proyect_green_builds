using System;

public enum DamageType
{
    Physical = 0,
    Magic = 1
}

public static class CombatDefense
{
    public const float RatingConstant = 100f;

    public static double DamageMultiplier(float rating)
    {
        return RatingConstant / (RatingConstant + Math.Max(0d, rating));
    }

    public static double Reduction(float rating)
    {
        return 1d - DamageMultiplier(rating);
    }

    public static int ResolveDamage(int amount, DamageType type, float armor, float magicResist)
    {
        if (amount <= 0)
            return 0;

        double damage = amount * DamageMultiplier(armor);
        if (type == DamageType.Magic)
            damage *= DamageMultiplier(magicResist);

        // Round once after all layers; finite defenses never grant immunity.
        return Math.Max(1, (int)Math.Round(damage, MidpointRounding.AwayFromZero));
    }
}
