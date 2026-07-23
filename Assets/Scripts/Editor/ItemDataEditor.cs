using UnityEditor;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    private SerializedProperty itemTypeProperty;
    private SerializedProperty weaponHandednessProperty;
    private SerializedProperty armorCategoryProperty;

    private void OnEnable()
    {
        itemTypeProperty = serializedObject.FindProperty("itemType");
        weaponHandednessProperty = serializedObject.FindProperty("weaponHandedness");
        armorCategoryProperty = serializedObject.FindProperty("armorCategory");
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
}
