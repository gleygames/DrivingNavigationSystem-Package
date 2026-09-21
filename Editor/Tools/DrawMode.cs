using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    public class DrawMode : IRoadEditorMode
    {
        public void OnEnter()
        {
        }

        public void OnExit()
        {
        }

        public void OnWindowGUI()
        {
            EditorGUILayout.HelpBox("Draw mode is not implemented yet.", MessageType.Info);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }
    }
}
