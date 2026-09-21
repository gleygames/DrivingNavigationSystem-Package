using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    public class EditMode : IRoadEditorMode
    {
        public void OnEnter()
        {
        }

        public void OnExit()
        {
        }

        public void OnWindowGUI()
        {
            EditorGUILayout.HelpBox("Edit mode is not implemented yet.", MessageType.Info);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }
    }
}
