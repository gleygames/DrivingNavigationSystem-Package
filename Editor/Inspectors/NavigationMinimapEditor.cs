using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    [CustomEditor(typeof(NavigationMinimap))]
    public class NavigationMinimapEditor : UnityEditor.Editor
    {
        private readonly RootInspectorDrawer drawer = new RootInspectorDrawer();
        private readonly string[] visiblePaths =
        {
            "manager",
            "shapeSettings.shapeKind",
            "shapeSettings.sprite",
            "shapeSettings.spriteOutline",
            "followSettings.rotationMode",
            "followSettings.carOffsetFromBottom",
            "followSettings.speedZoom",
            "followSettings.speedZoomMinMeters",
            "followSettings.speedZoomMaxMeters",
            "followSettings.fixedZoomMeters",
            "tapAction",
            "fullMap",
            "viewSettings.showOffScreenArrows",
            "viewSettings.showArrowDistance",
            "viewSettings.showMarkerLabels",
            "compassButton",
            "compassIcon"
        };
        private readonly string[] advancedPaths =
        {
            "followSettings.rotationSmoothing",
            "followSettings.turnSmoothing",
            "followSettings.turnAngleThreshold",
            "followSettings.noseDeadZoneDegrees",
            "followSettings.zoomSmoothing",
            "followSettings.speedZoomSlowSpeed",
            "followSettings.speedZoomFastSpeed",
            "viewSettings.minZoomMeters",
            "viewSettings.edgeInset",
            "viewSettings.routeStyle",
            "viewSettings.arrowPrefab",
            "viewSettings.textWriter",
            "viewport",
            "viewSettings.showPreview",
            "viewSettings.channelMask"
        };
        private bool advancedOpen;

        internal string[] VisiblePaths { get { return visiblePaths; } }
        internal string[] AdvancedPaths { get { return advancedPaths; } }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            drawer.DrawProperty(serializedObject, "manager");

            drawer.DrawHeader("Shape");
            drawer.DrawProperty(serializedObject, "shapeSettings.shapeKind");
            SerializedProperty shapeKind = serializedObject.FindProperty("shapeSettings.shapeKind");
            if (shapeKind != null && shapeKind.intValue == (int)MinimapShapeKind.Sprite)
            {
                drawer.DrawProperty(serializedObject, "shapeSettings.sprite");
                drawer.DrawProperty(serializedObject, "shapeSettings.spriteOutline");
            }

            drawer.DrawHeader("Rotation");
            drawer.DrawProperty(serializedObject, "followSettings.rotationMode");
            drawer.DrawProperty(serializedObject, "followSettings.carOffsetFromBottom");

            drawer.DrawHeader("Zoom");
            drawer.DrawProperty(serializedObject, "followSettings.speedZoom");
            SerializedProperty speedZoom = serializedObject.FindProperty("followSettings.speedZoom");
            if (speedZoom != null && speedZoom.boolValue)
            {
                drawer.DrawProperty(serializedObject, "followSettings.speedZoomMinMeters");
                drawer.DrawProperty(serializedObject, "followSettings.speedZoomMaxMeters");
            }
            else
            {
                drawer.DrawProperty(serializedObject, "followSettings.fixedZoomMeters");
            }

            drawer.DrawHeader("Tap");
            drawer.DrawProperty(serializedObject, "tapAction");
            SerializedProperty tapAction = serializedObject.FindProperty("tapAction");
            if (tapAction != null && tapAction.intValue == (int)MinimapTapAction.OpenFullMap)
            {
                drawer.DrawProperty(serializedObject, "fullMap");
                EditorGUILayout.LabelField("Empty = found automatically", EditorStyles.miniLabel);
            }

            drawer.DrawHeader("Markers");
            drawer.DrawProperty(serializedObject, "viewSettings.showMarkerLabels");

            drawer.DrawHeader("Off-screen arrows");
            drawer.DrawProperty(serializedObject, "viewSettings.showOffScreenArrows");
            SerializedProperty showArrows = serializedObject.FindProperty("viewSettings.showOffScreenArrows");
            if (showArrows != null && showArrows.boolValue)
            {
                drawer.DrawProperty(serializedObject, "viewSettings.showArrowDistance");
            }

            drawer.DrawHeader("Compass");
            drawer.DrawProperty(serializedObject, "compassButton");
            drawer.DrawProperty(serializedObject, "compassIcon");

            advancedOpen = drawer.DrawAdvancedFoldout(advancedOpen);
            if (advancedOpen)
            {
                EditorGUI.indentLevel++;
                drawer.DrawProperties(serializedObject, advancedPaths);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
