using UnityEditor;
using UnityEngine;
using Zodiac.Util;

namespace Zodiac.Editor
{
    [CustomPropertyDrawer(typeof(LabelOverride))]
    public class LabelOverridePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            try
            {
                var propertyAttribute = (LabelOverride)base.attribute;
                if (!IsItBloodyArrayTho(property))
                {
                    label.text = propertyAttribute!.label;
                }
                else
                {
                    Debug.LogWarningFormat(
                        "{0}(\"{1}\") doesn't support arrays ",
                        nameof(LabelOverride),
                        propertyAttribute!.label
                    );
                }

                EditorGUI.PropertyField(position, property, label);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        bool IsItBloodyArrayTho(SerializedProperty property)
        {
            string path = property.propertyPath;
            int idot = path.IndexOf('.');
            if (idot is -1) return false;
            string propName = path[..idot];
            SerializedProperty p = property.serializedObject.FindProperty(propName);
            return p.isArray;
            //CREDITS: https://answers.unity.com/questions/603882/serializedproperty-isnt-being-detected-as-an-array.html
        }
    }
}
