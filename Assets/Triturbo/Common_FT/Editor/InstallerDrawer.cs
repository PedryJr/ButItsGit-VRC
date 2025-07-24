using UnityEngine;
using UnityEditor;

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Triturbo.FaceTrackingAddon
{

    [CustomPropertyDrawer(typeof(FaceTrackingAddonData.Installer))]
    public class InstallerDrawer : PropertyDrawer
    {
        bool init = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);



            // Calculate the height of each property field
            float lineHeight = EditorGUIUtility.singleLineHeight;
            position.height = lineHeight;

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Get the child properties of the Installer class
            SerializedProperty prefabProperty = property.FindPropertyRelative("prefab");
            SerializedProperty nameProperty = property.FindPropertyRelative("name");
            SerializedProperty costProperty = property.FindPropertyRelative("cost");


            //position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);


            // Draw prefab and name fields
            EditorGUI.PropertyField(position, prefabProperty);


            if (prefabProperty.serializedObject.hasModifiedProperties)
            {
                costProperty.intValue = ParameterHelper.GetMAParameterMemoryUsed(prefabProperty.objectReferenceValue as GameObject);

                costProperty.serializedObject.ApplyModifiedProperties();
   
            }

            position.y += lineHeight;
            EditorGUI.PropertyField(position, nameProperty);

            // Move to the next line
            position.y += lineHeight;

            // Draw cost as a read-only field
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, costProperty);
            EditorGUI.EndDisabledGroup();
            

            EditorGUI.EndProperty();
        }

        // Calculate the height of the property drawer
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3; // Assuming three fields are drawn
        }



    }
}


