using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ItemStat))]
public class ItemStatDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty statTypeProperty = property.FindPropertyRelative("statType");
        SerializedProperty valueTypeProperty = property.FindPropertyRelative("valueType");
        SerializedProperty valueProperty = property.FindPropertyRelative("value");

        position.height = EditorGUIUtility.singleLineHeight;
        Rect contentPosition = EditorGUI.PrefixLabel(position, label);

        float spacing = 4f;
        float statWidth = contentPosition.width * 0.4f;
        float typeWidth = contentPosition.width * 0.34f;
        float valueWidth = contentPosition.width - statWidth - typeWidth - spacing * 2f;

        Rect statRect = new Rect(contentPosition.x, contentPosition.y, statWidth, contentPosition.height);
        Rect typeRect = new Rect(statRect.xMax + spacing, contentPosition.y, typeWidth, contentPosition.height);
        Rect valueRect = new Rect(typeRect.xMax + spacing, contentPosition.y, valueWidth, contentPosition.height);

        EditorGUI.PropertyField(statRect, statTypeProperty, GUIContent.none);
        EditorGUI.PropertyField(typeRect, valueTypeProperty, GUIContent.none);
        EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);

        EditorGUI.EndProperty();
    }
}
