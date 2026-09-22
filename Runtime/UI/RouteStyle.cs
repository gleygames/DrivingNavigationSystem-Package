using UnityEngine;

namespace Gley.NavigationSystem
{
    public class RouteStyle : ScriptableObject, IFormatVersioned
    {
        public const int CurrentFormatVersion = 1;

        [SerializeField] private Shader lineShader;
        [SerializeField] private Color activeLineColor = new Color(0.16f, 0.47f, 1f, 1f);
        [SerializeField] private Color activeOutlineColor = new Color(0.05f, 0.2f, 0.55f, 1f);
        [SerializeField] private Color previewLineColor = new Color(0.6f, 0.8f, 1f, 0.6f);
        [SerializeField] private Color previewOutlineColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        [SerializeField] private Color fadedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private RouteDrivenMode drivenMode = RouteDrivenMode.Removed;
        [SerializeField] private float halfWidth = 3f;
        [SerializeField] private float outlineWidth = 1.5f;
        [SerializeField] private float dashLength = 6f;
        [SerializeField] private float gapLength = 4f;
        [SerializeField] private int formatVersion = CurrentFormatVersion;

        public Shader LineShader { get { return lineShader; } }
        public Color ActiveLineColor { get { return activeLineColor; } }
        public Color ActiveOutlineColor { get { return activeOutlineColor; } }
        public Color PreviewLineColor { get { return previewLineColor; } }
        public Color PreviewOutlineColor { get { return previewOutlineColor; } }
        public Color FadedColor { get { return fadedColor; } }
        public RouteDrivenMode DrivenMode { get { return drivenMode; } }
        public float HalfWidth { get { return halfWidth; } }
        public float OutlineWidth { get { return outlineWidth; } }
        public float DashLength { get { return dashLength; } }
        public float GapLength { get { return gapLength; } }
        public int FormatVersion { get { return formatVersion; } }
        int IFormatVersioned.CurrentFormatVersion { get { return CurrentFormatVersion; } }

        internal void SetFormatVersion(int value)
        {
            formatVersion = value;
        }
    }
}
