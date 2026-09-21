using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    public class NavigationEditorPrefs
    {
        private const string Prefix = "Gley.Navigation.";

        public bool ShowMapOverlay
        {
            get { return EditorPrefs.GetBool(Prefix + "ShowMapOverlay", false); }
            set { EditorPrefs.SetBool(Prefix + "ShowMapOverlay", value); }
        }

        public bool OverlayAlwaysOnTop
        {
            get { return EditorPrefs.GetBool(Prefix + "OverlayAlwaysOnTop", true); }
            set { EditorPrefs.SetBool(Prefix + "OverlayAlwaysOnTop", value); }
        }
    }
}
