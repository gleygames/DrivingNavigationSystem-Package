using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    public interface IRoadEditorMode
    {
        void OnEnter();
        void OnExit();
        void OnWindowGUI();
        void OnSceneGUI(SceneView sceneView);
    }
}
