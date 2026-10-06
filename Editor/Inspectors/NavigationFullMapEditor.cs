using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    [CustomEditor(typeof(NavigationFullMap))]
    public class NavigationFullMapEditor : UnityEditor.Editor
    {
        private readonly RootInspectorDrawer drawer = new RootInspectorDrawer();
        private readonly NavigationTextOutput textOutput = new NavigationTextOutput();
        private readonly string[] visiblePaths =
        {
            "manager",
            "interactionSettings.builtInPointerInput",
            "interactionSettings.confirmStep",
            "interactionSettings.tapTarget",
            "interactionSettings.openZoomMeters",
            "interactionSettings.zoomOutMode",
            "interactionSettings.crosshairMode",
            "interactionSettings.fling",
            "interactionSettings.doubleTapZoom",
            "viewSettings.showOffScreenArrows",
            "viewSettings.showArrowDistance",
            "previewPanel.panelRoot",
            "previewPanel.distanceText",
            "previewPanel.etaText",
            "previewPanel.confirmButton",
            "previewPanel.cancelButton",
            "buttons.stopButton",
            "buttons.centerButton",
            "buttons.closeButton",
            "crosshairImage"
        };
        private readonly string[] advancedPaths =
        {
            "interactionSettings.mouseWheelStep",
            "interactionSettings.doubleTapStep",
            "interactionSettings.markerTapRadius",
            "viewSettings.minZoomMeters",
            "viewSettings.edgeInset",
            "viewSettings.routeStyle",
            "viewSettings.arrowPrefab",
            "viewSettings.textWriter",
            "viewport",
            "viewSettings.channelMask",
            "viewSettings.showPreview"
        };
        private bool advancedOpen;

        internal string[] VisiblePaths { get { return visiblePaths; } }
        internal string[] AdvancedPaths { get { return advancedPaths; } }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            drawer.DrawProperty(serializedObject, "manager");

            drawer.DrawHeader("Interaction");
            drawer.DrawProperty(serializedObject, "interactionSettings.builtInPointerInput");
            drawer.DrawProperty(serializedObject, "interactionSettings.confirmStep");
            drawer.DrawProperty(serializedObject, "interactionSettings.tapTarget");
            SerializedProperty tapTarget = serializedObject.FindProperty("interactionSettings.tapTarget");
            if (tapTarget != null && tapTarget.enumValueIndex == (int)FullMapTapTarget.MarkersOnly)
            {
                EditorGUILayout.HelpBox("Only markers with Can Be Destination on start a route. Tap radius: Advanced > Marker Tap Radius.", MessageType.Info);
            }

            drawer.DrawProperty(serializedObject, "interactionSettings.openZoomMeters");
            drawer.DrawProperty(serializedObject, "interactionSettings.zoomOutMode");
            drawer.DrawProperty(serializedObject, "interactionSettings.crosshairMode");

            drawer.DrawHeader("Gestures");
            drawer.DrawProperty(serializedObject, "interactionSettings.fling");
            drawer.DrawProperty(serializedObject, "interactionSettings.doubleTapZoom");

            drawer.DrawHeader("Off-screen arrows");
            drawer.DrawProperty(serializedObject, "viewSettings.showOffScreenArrows");
            SerializedProperty showArrows = serializedObject.FindProperty("viewSettings.showOffScreenArrows");
            if (showArrows != null && showArrows.boolValue)
            {
                drawer.DrawProperty(serializedObject, "viewSettings.showArrowDistance");
            }

            drawer.DrawHeader("Preview panel");
            drawer.DrawProperty(serializedObject, "previewPanel.panelRoot");
            drawer.DrawProperty(serializedObject, "previewPanel.distanceText");
            DrawTextSlotWarning("previewPanel.distanceText");
            drawer.DrawProperty(serializedObject, "previewPanel.etaText");
            DrawTextSlotWarning("previewPanel.etaText");
            drawer.DrawProperty(serializedObject, "previewPanel.confirmButton");
            drawer.DrawProperty(serializedObject, "previewPanel.cancelButton");

            drawer.DrawHeader("Buttons");
            drawer.DrawProperty(serializedObject, "buttons.stopButton");
            drawer.DrawProperty(serializedObject, "buttons.centerButton");
            drawer.DrawProperty(serializedObject, "buttons.closeButton");

            drawer.DrawHeader("Crosshair");
            drawer.DrawProperty(serializedObject, "crosshairImage");

            advancedOpen = drawer.DrawAdvancedFoldout(advancedOpen);
            if (advancedOpen)
            {
                EditorGUI.indentLevel++;
                drawer.DrawProperties(serializedObject, advancedPaths);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTextSlotWarning(string path)
        {
            SerializedProperty slot = serializedObject.FindProperty(path);
            if (slot == null)
            {
                return;
            }

            Component component = slot.objectReferenceValue as Component;
            if (component == null)
            {
                return;
            }

            NavigationTextWriter writer = null;
            SerializedProperty writerProperty = serializedObject.FindProperty("viewSettings.textWriter");
            if (writerProperty != null)
            {
                writer = writerProperty.objectReferenceValue as NavigationTextWriter;
            }

            if (!textOutput.CanWrite(component, writer))
            {
                EditorGUILayout.HelpBox("This component can't show text. Assign a TextMeshPro or legacy Text, or set Advanced > Text Writer.", MessageType.Error);
            }
        }
    }
}
