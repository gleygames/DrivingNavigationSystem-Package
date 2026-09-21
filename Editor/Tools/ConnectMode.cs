using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    public class ConnectMode : IRoadEditorMode
    {
        public void OnEnter()
        {
        }

        public void OnExit()
        {
        }

        public void OnWindowGUI()
        {
            EditorGUILayout.HelpBox("Connect mode is not implemented yet.", MessageType.Info);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }
    }
}
