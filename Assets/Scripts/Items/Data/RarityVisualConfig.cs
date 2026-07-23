using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "RarityVisualConfig", menuName = "Game/UI/Rarity Visual Config")]
public class RarityVisualConfig : ScriptableObject
{
    [System.Serializable]
    public class RarityVisualEntry
    {
        public ItemRarity rarity;
        public Sprite backgroundSprite;
        public bool useBackgroundColorTint;
        public Color backgroundColor = Color.white;
        public bool preserveAspect = true;
    }

    [SerializeField] private List<RarityVisualEntry> entries = new();

    public RarityVisualEntry GetVisual(ItemRarity rarity)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            RarityVisualEntry entry = entries[i];
            if (entry != null && entry.rarity == rarity)
                return entry;
        }

        return null;
    }

    public void ApplyTo(Image targetImage, ItemRarity rarity)
    {
        if (targetImage == null)
            return;

        RarityVisualEntry entry = GetVisual(rarity);
        if (entry == null)
        {
            targetImage.sprite = null;
            targetImage.color = Color.white;
            targetImage.preserveAspect = false;
            return;
        }

        targetImage.sprite = entry.backgroundSprite;
        targetImage.color = entry.useBackgroundColorTint ? entry.backgroundColor : Color.white;
        targetImage.preserveAspect = entry.preserveAspect;
    }
}
