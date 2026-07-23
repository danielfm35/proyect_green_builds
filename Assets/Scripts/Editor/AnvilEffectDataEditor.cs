using UnityEditor;

[CustomEditor(typeof(AnvilEffectData))]
public class AnvilEffectDataEditor : Editor
{
    private SerializedProperty idProperty;
    private SerializedProperty effectNameProperty;
    private SerializedProperty descriptionProperty;
    private SerializedProperty localizedNameProperty;
    private SerializedProperty localizedDescriptionProperty;
    private SerializedProperty iconProperty;
    private SerializedProperty backgroundProperty;
    private SerializedProperty backgroundColorProperty;
    private SerializedProperty kindProperty;
    private SerializedProperty actionProperty;
    private SerializedProperty effectTypeProperty;
    private SerializedProperty rarityStepsProperty;
    private SerializedProperty spawnWeightProperty;

    private void OnEnable()
    {
        idProperty = serializedObject.FindProperty("id");
        effectNameProperty = serializedObject.FindProperty("effectName");
        descriptionProperty = serializedObject.FindProperty("description");
        localizedNameProperty = serializedObject.FindProperty("localizedName");
        localizedDescriptionProperty = serializedObject.FindProperty("localizedDescription");
        iconProperty = serializedObject.FindProperty("icon");
        backgroundProperty = serializedObject.FindProperty("background");
        backgroundColorProperty = serializedObject.FindProperty("backgroundColor");
        kindProperty = serializedObject.FindProperty("kind");
        actionProperty = serializedObject.FindProperty("action");
        effectTypeProperty = serializedObject.FindProperty("effectType");
        rarityStepsProperty = serializedObject.FindProperty("raritySteps");
        spawnWeightProperty = serializedObject.FindProperty("spawnWeight");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(idProperty);
        EditorGUILayout.PropertyField(effectNameProperty);
        EditorGUILayout.PropertyField(descriptionProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Localization", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(localizedNameProperty);
        EditorGUILayout.PropertyField(localizedDescriptionProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(iconProperty);
        EditorGUILayout.PropertyField(backgroundProperty);
        EditorGUILayout.PropertyField(backgroundColorProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effect", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(kindProperty);
        EditorGUILayout.PropertyField(actionProperty);

        if (actionProperty.objectReferenceValue == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Legacy Fallback", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(effectTypeProperty);

            AnvilEffectType effectType = (AnvilEffectType)effectTypeProperty.enumValueIndex;
            if (UsesRaritySteps(effectType))
                EditorGUILayout.PropertyField(rarityStepsProperty);
        }

        EditorGUILayout.PropertyField(spawnWeightProperty);

        serializedObject.ApplyModifiedProperties();
    }

    private bool UsesRaritySteps(AnvilEffectType effectType)
    {
        return effectType == AnvilEffectType.IncreaseResultRarity ||
               effectType == AnvilEffectType.DecreaseResultRarity;
    }
}
