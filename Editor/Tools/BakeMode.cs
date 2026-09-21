using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    public class BakeMode : IRoadEditorMode
    {
        public void OnEnter()
        {
        }

        public void OnExit()
        {
        }

        public void OnWindowGUI()
        {
            EditorGUILayout.HelpBox("Bake mode is not implemented yet.", MessageType.Info);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }
    }
}
