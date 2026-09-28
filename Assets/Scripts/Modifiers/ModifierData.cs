using UnityEngine;

[System.Serializable]
public sealed class ModifierInstance
{
    public ModifierData data;

    public ModifierInstance(ModifierData modifierData)
    {
        data = modifierData;
    }
}

[CreateAssetMenu(fileName = "NewModifier", menuName = "Game/Modifiers/Modifier")]
public sealed class ModifierData : ScriptableObject
{
    public enum ModifierProperty
    {
        PlacementRule,
        CrafterSide,
    }

    public string modifierName;
    public string id;

    [TextArea(2, 5)]
    public string description;

    public Sprite icon;
    public ModifierProperty property;
    public GridPlacementRule placementRule;
    public CrafterSide crafterSide;
}
