using UnityEngine;
using UnityEditor;

namespace Gley.NavigationSystem.Editor
{
    public class NavigationEditorPrefs
    {
        private const string Prefix = "Gley.Navigation.";

        public LayerMask RoadLayers
        {
            get { return EditorPrefs.GetInt(Prefix + "RoadLayers", -1); }
            set { EditorPrefs.SetInt(Prefix + "RoadLayers", value.value); }
        }

        public string CaptureSettingsJson
        {
            get { return EditorPrefs.GetString(Prefix + "CaptureSettings", string.Empty); }
            set { EditorPrefs.SetString(Prefix + "CaptureSettings", value); }
        }

        public float SnapDistance
        {
            get { return EditorPrefs.GetFloat(Prefix + "SnapDistance", 3f); }
            set { EditorPrefs.SetFloat(Prefix + "SnapDistance", value); }
        }

        public float MaxDeviation
        {
            get { return EditorPrefs.GetFloat(Prefix + "MaxDeviation", 0.1f); }
            set { EditorPrefs.SetFloat(Prefix + "MaxDeviation", value); }
        }

        public float MaxSpacing
        {
            get { return EditorPrefs.GetFloat(Prefix + "MaxSpacing", 20f); }
            set { EditorPrefs.SetFloat(Prefix + "MaxSpacing", value); }
        }

        public float BrushSpeedOverride
        {
            get { return EditorPrefs.GetFloat(Prefix + "BrushSpeedOverride", 0f); }
            set { EditorPrefs.SetFloat(Prefix + "BrushSpeedOverride", value); }
        }

        public float BrushWidthOverride
        {
            get { return EditorPrefs.GetFloat(Prefix + "BrushWidthOverride", 0f); }
            set { EditorPrefs.SetFloat(Prefix + "BrushWidthOverride", value); }
        }

        public int LastMode
        {
            get { return EditorPrefs.GetInt(Prefix + "LastMode", 0); }
            set { EditorPrefs.SetInt(Prefix + "LastMode", value); }
        }

        public int BrushTypeId
        {
            get { return EditorPrefs.GetInt(Prefix + "BrushTypeId", 0); }
            set { EditorPrefs.SetInt(Prefix + "BrushTypeId", value); }
        }

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

        public bool ShowRoadLines
        {
            get { return EditorPrefs.GetBool(Prefix + "ShowRoadLines", true); }
            set { EditorPrefs.SetBool(Prefix + "ShowRoadLines", value); }
        }

        public bool ShowDirectionArrows
        {
            get { return EditorPrefs.GetBool(Prefix + "ShowDirectionArrows", true); }
            set { EditorPrefs.SetBool(Prefix + "ShowDirectionArrows", value); }
        }

        public bool ShowRoadTypeColors
        {
            get { return EditorPrefs.GetBool(Prefix + "ShowRoadTypeColors", true); }
            set { EditorPrefs.SetBool(Prefix + "ShowRoadTypeColors", value); }
        }

        public bool ShowKeyPoints
        {
            get { return EditorPrefs.GetBool(Prefix + "ShowKeyPoints", true); }
            set { EditorPrefs.SetBool(Prefix + "ShowKeyPoints", value); }
        }

        public bool ShowDenseShapePoints
        {
            get { return EditorPrefs.GetBool(Prefix + "ShowDenseShapePoints", false); }
            set { EditorPrefs.SetBool(Prefix + "ShowDenseShapePoints", value); }
        }

        public bool ShowIntersections
        {
            get { return EditorPrefs.GetBool(Prefix + "ShowIntersections", true); }
            set { EditorPrefs.SetBool(Prefix + "ShowIntersections", value); }
        }

        public bool ShowValidationHighlights
        {
            get { return EditorPrefs.GetBool(Prefix + "ShowValidationHighlights", true); }
            set { EditorPrefs.SetBool(Prefix + "ShowValidationHighlights", value); }
        }

        public bool ShowMapRectangle
        {
            get { return EditorPrefs.GetBool(Prefix + "ShowMapRectangle", true); }
            set { EditorPrefs.SetBool(Prefix + "ShowMapRectangle", value); }
        }

        public bool BrushOneWay
        {
            get { return EditorPrefs.GetBool(Prefix + "BrushOneWay", false); }
            set { EditorPrefs.SetBool(Prefix + "BrushOneWay", value); }
        }
    }
}
