using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    internal class RootInspectorDrawer
    {
        internal void DrawHeader(string title)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        internal void DrawProperty(SerializedObject serializedObject, string path)
        {
            SerializedProperty property = serializedObject.FindProperty(path);
            if (property == null)
            {
                EditorGUILayout.HelpBox("Missing property: " + path, MessageType.Error);
                return;
            }

            EditorGUILayout.PropertyField(property);
        }

        internal void DrawProperties(SerializedObject serializedObject, string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                DrawProperty(serializedObject, paths[i]);
            }
        }

        internal bool DrawAdvancedFoldout(bool open)
        {
            EditorGUILayout.Space();
            return EditorGUILayout.Foldout(open, "Advanced", true);
        }
    }
}
