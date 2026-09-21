using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    public class ValidateMode : IRoadEditorMode
    {
        public void OnEnter()
        {
        }

        public void OnExit()
        {
        }

        public void OnWindowGUI()
        {
            EditorGUILayout.HelpBox("Validate mode is not implemented yet.", MessageType.Info);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }
    }
}
