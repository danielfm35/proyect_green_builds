using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    private SerializedProperty itemTypeProperty;
    private SerializedProperty weaponHandednessProperty;
    private SerializedProperty armorCategoryProperty;
    private SerializedProperty levelProperty;
    private SerializedProperty abilitiesProperty;
    private readonly List<AbilityData> availableAbilities = new();

    private void OnEnable()
    {
        itemTypeProperty = serializedObject.FindProperty("itemType");
        weaponHandednessProperty = serializedObject.FindProperty("weaponHandedness");
        armorCategoryProperty = serializedObject.FindProperty("armorCategory");
        levelProperty = serializedObject.FindProperty("level");
        abilitiesProperty = serializedObject.FindProperty("abilities");

        ReloadAvailableAbilities();
        EditorApplication.projectChanged += OnProjectChanged;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= OnProjectChanged;
    }

    private void OnProjectChanged()
    {
        ReloadAvailableAbilities();
        Repaint();
    }

    private void ReloadAvailableAbilities()
    {
        availableAbilities.Clear();
        availableAbilities.AddRange(Resources.LoadAll<AbilityData>("ScriptableObjects/Abilities"));
        availableAbilities.Sort((left, right) =>
            string.Compare(GetAbilityDisplayName(left), GetAbilityDisplayName(right), System.StringComparison.OrdinalIgnoreCase));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.name == "weaponHandedness" || property.name == "armorCategory")
                continue;

            if (property.name == "abilities")
            {
                DrawAbilities();
                continue;
            }

            using (new EditorGUI.DisabledScope(property.name == "m_Script"))
            {
                EditorGUILayout.PropertyField(property, true);
            }

            if (property.name == "itemType" && itemTypeProperty.enumValueIndex == (int)ItemType.Weapon)
                EditorGUILayout.PropertyField(weaponHandednessProperty);

            if (property.name == "itemType" && itemTypeProperty.enumValueIndex == (int)ItemType.Armor)
                EditorGUILayout.PropertyField(armorCategoryProperty);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAbilities()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            new GUIContent("Abilities", "Abilities loaded from Resources/ScriptableObjects/Abilities."),
            EditorStyles.boldLabel);

        int maxAbilities = Mathf.Max(0, levelProperty.intValue);
        if (maxAbilities == 0)
        {
            EditorGUILayout.HelpBox("This item does not allow abilities because its level is 0.", MessageType.Info);
            return;
        }

        if (availableAbilities.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No AbilityData assets were found in Resources/ScriptableObjects/Abilities.",
                MessageType.Warning);
            return;
        }

        for (int slotIndex = 0; slotIndex < maxAbilities; slotIndex++)
            DrawAbilitySlot(slotIndex);
    }

    private void DrawAbilitySlot(int slotIndex)
    {
        AbilityData current = slotIndex < abilitiesProperty.arraySize
            ? abilitiesProperty.GetArrayElementAtIndex(slotIndex).objectReferenceValue as AbilityData
            : null;

        List<AbilityData> options = new() { null };
        List<string> optionNames = new() { "None" };

        for (int i = 0; i < availableAbilities.Count; i++)
        {
            AbilityData ability = availableAbilities[i];
            if (ability == null || (ability != current && IsAssignedInAnotherSlot(ability, slotIndex)))
                continue;

            options.Add(ability);
            optionNames.Add(GetAbilityDisplayName(ability));
        }

        if (current != null && !options.Contains(current))
        {
            options.Add(current);
            optionNames.Add(GetAbilityDisplayName(current));
        }

        int currentIndex = Mathf.Max(0, options.IndexOf(current));
        int selectedIndex = EditorGUILayout.Popup($"Slot {slotIndex + 1}", currentIndex, optionNames.ToArray());

        if (selectedIndex == currentIndex)
            return;

        AbilityData selected = options[selectedIndex];
        if (selected == null)
        {
            RemoveAbilityAt(slotIndex);
            return;
        }

        while (abilitiesProperty.arraySize <= slotIndex)
            abilitiesProperty.InsertArrayElementAtIndex(abilitiesProperty.arraySize);

        abilitiesProperty.GetArrayElementAtIndex(slotIndex).objectReferenceValue = selected;
    }

    private bool IsAssignedInAnotherSlot(AbilityData ability, int currentSlot)
    {
        for (int i = 0; i < abilitiesProperty.arraySize; i++)
        {
            if (i == currentSlot)
                continue;

            if (abilitiesProperty.GetArrayElementAtIndex(i).objectReferenceValue == ability)
                return true;
        }

        return false;
    }

    private void RemoveAbilityAt(int index)
    {
        if (index >= abilitiesProperty.arraySize)
            return;

        int previousSize = abilitiesProperty.arraySize;
        abilitiesProperty.DeleteArrayElementAtIndex(index);

        if (abilitiesProperty.arraySize == previousSize)
            abilitiesProperty.DeleteArrayElementAtIndex(index);
    }

    private static string GetAbilityDisplayName(AbilityData ability)
    {
        if (ability == null)
            return "None";

        return string.IsNullOrWhiteSpace(ability.abilityName) ? ability.name : ability.abilityName;
    }
}
