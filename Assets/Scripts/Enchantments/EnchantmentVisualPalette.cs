using UnityEngine;

public static class EnchantmentVisualPalette
{
    private static readonly Color[] Colors =
    {
        new Color(0.15f, 1f, 0.3f, 1f),
        new Color(0.1f, 0.75f, 1f, 1f),
        new Color(1f, 0.2f, 0.85f, 1f)
    };

    public static Color GetColor(int enchantmentIndex)
    {
        return Colors[Mathf.Abs(enchantmentIndex) % Colors.Length];
    }
}
