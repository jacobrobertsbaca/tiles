using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tiles.Puzzles.Power.Editor
{
    [CustomPropertyDrawer(typeof(PowerNode))]
    public class PowerNodePropertyDrawer : PropertyDrawer
    {
        private static class Styles
        {
            public static float ButtonWidth = 16f;
            public static GUIContent ButtonContent = EditorGUIUtility.IconContent("Preset.Context");
        }

        private const string nodeIndexName = "nodeIndex";
        private static readonly FieldInfo nodeIndexField = typeof(PowerNode).GetField(nodeIndexName, BindingFlags.NonPublic | BindingFlags.Instance);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty nodeIndex = property.FindPropertyRelative(nodeIndexName);
            PowerNode current = new PowerNode(nodeIndex.intValue);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            Rect textRect = new Rect(position.x, position.y, position.width - Styles.ButtonWidth, position.height);
            Rect buttonRect = new Rect(position.x + position.width - Styles.ButtonWidth, position.y, Styles.ButtonWidth, position.height);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.TextField(textRect, current.ToString());
            }

            if (GUI.Button(buttonRect, Styles.ButtonContent, EditorStyles.miniButtonRight))
            {
                PowerNodePicker.Show(current, pn => {
                    nodeIndex.intValue = (int) nodeIndexField.GetValue(pn);
                    nodeIndex.serializedObject.ApplyModifiedProperties();
                });
            }

            EditorGUI.EndProperty();
        }
    }
}
