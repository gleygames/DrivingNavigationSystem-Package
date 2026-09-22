using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class MapImagePanel
    {
        private const float MaxPreviewWidth = 512f;
        private const float MinPieceSizeMeters = 10f;
        private const int MinLongerSidePixels = 4;
        private const int MaxOverlapPx = 256;

        private readonly string[] templateSizeLabels;
        private readonly int[] templateSizeValues;
        private readonly MapCaptureExecutor executor;
        private readonly MapImageImportSettings importSettings;
        private readonly CustomImageAssigner customImageAssigner;
        private readonly TemplateExporter templateExporter;
        private readonly CapturePlanner planner;
        private readonly NavigationEditorPrefs prefs;
        private readonly CaptureSettings settings;

        private MapData pendingData;
        private Texture2D previewTexture;
        private ICapturePipelineAdapter adapter;
        private float pendingUnitsPerMeter;
        private int templateSizeIndex;
        private bool disposed;

        public MapImagePanel(NavigationEditorPrefs prefs)
        {
            this.prefs = prefs;
            executor = new MapCaptureExecutor();
            importSettings = new MapImageImportSettings();
            customImageAssigner = new CustomImageAssigner();
            templateExporter = new TemplateExporter();
            planner = new CapturePlanner();
            settings = new CaptureSettings();
            templateSizeLabels = new string[] { "2048 px", "4096 px" };
            templateSizeValues = new int[] { 2048, 4096 };

            string json = prefs.CaptureSettingsJson;
            if (!string.IsNullOrEmpty(json))
            {
                JsonUtility.FromJsonOverwrite(json, settings);
            }
        }

        public void Draw(MapData data, float unitsPerMeter)
        {
            if (data == null)
            {
                EditorGUILayout.HelpBox("No map data assigned.", MessageType.Info);
                return;
            }

            EnsureAdapter();
            DrawStatus(data);

            EditorGUI.BeginChangeCheck();
            DrawSettingsFields();
            if (EditorGUI.EndChangeCheck())
            {
                prefs.CaptureSettingsJson = JsonUtility.ToJson(settings);
            }

            DrawResolutionWarning();
            DrawPreview(data, unitsPerMeter);
            DrawCaptureButton(data, unitsPerMeter);
            DrawChangeAreaButton(data);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Image", EditorStyles.boldLabel);
            DrawCustomImageField(data);
            DrawGuidance(data);
            DrawTemplateExport(data);
        }

        private void EnsureAdapter()
        {
            if (adapter != null && adapter.IsActive())
            {
                return;
            }
            adapter = executor.FindActiveAdapter();
        }

        private void DrawStatus(MapData data)
        {
            EditorGUILayout.LabelField("Image", data.ImageState.ToString());

            string lockText;
            if (data.Locked)
            {
                lockText = "Locked";
            }
            else
            {
                lockText = "Unlocked";
            }
            EditorGUILayout.LabelField("Area", lockText);

            if (data.ImageState == MapImageState.Outdated)
            {
                EditorGUILayout.HelpBox("The map area changed after the capture. Recapture the map image.", MessageType.Warning);
            }
        }

        private void DrawSettingsFields()
        {
            int longerSide = EditorGUILayout.IntField("Resolution (longer side)", settings.LongerSidePixels);
            settings.LongerSidePixels = Mathf.Clamp(longerSide, MinLongerSidePixels, CaptureSettings.MaxLongerSidePixels);

            float pieceSize = EditorGUILayout.FloatField("Piece Size (m)", settings.PieceSizeMeters);
            settings.PieceSizeMeters = Mathf.Max(MinPieceSizeMeters, pieceSize);

            int overlap = EditorGUILayout.IntField("Piece Overlap (px)", settings.PieceOverlapPx);
            settings.PieceOverlapPx = Mathf.Clamp(overlap, 0, MaxOverlapPx);

            int layerMask = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(settings.Layers);
            int newLayerMask = EditorGUILayout.MaskField("Layers", layerMask, InternalEditorUtility.layers);
            settings.Layers = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newLayerMask);

            settings.DisableFog = EditorGUILayout.Toggle("Disable Fog", settings.DisableFog);
            settings.DisablePostEffects = EditorGUILayout.Toggle("Disable Post Effects", settings.DisablePostEffects);
            settings.FillColor = EditorGUILayout.ColorField("Fill Color", settings.FillColor);

            DrawExposureField();

            settings.UseOwnCamera = EditorGUILayout.Toggle("Use My Own Camera", settings.UseOwnCamera);
            if (settings.UseOwnCamera)
            {
                settings.OwnCamera = (Camera)EditorGUILayout.ObjectField("Camera", settings.OwnCamera, typeof(Camera), true);
            }
        }

        private void DrawExposureField()
        {
            if (!adapter.SupportsExposure)
            {
                return;
            }

            if (!settings.HdrpExposureEdited)
            {
                float sceneExposure;
                if (adapter.TryGetDefaultExposure(out sceneExposure))
                {
                    settings.HdrpExposure = sceneExposure;
                }
                else
                {
                    settings.HdrpExposure = CaptureSettings.DefaultHdrpExposure;
                }
            }

            float newExposure = EditorGUILayout.FloatField("Capture Exposure (EV)", settings.HdrpExposure);
            if (!Mathf.Approximately(newExposure, settings.HdrpExposure))
            {
                settings.HdrpExposure = newExposure;
                settings.HdrpExposureEdited = true;
            }
        }

        private void DrawResolutionWarning()
        {
            if (settings.LongerSidePixels <= MapImageImportSettings.MobileMaxTextureSize)
            {
                return;
            }
            EditorGUILayout.HelpBox("Resolution above " + MapImageImportSettings.MobileMaxTextureSize + ": mobile builds will use a reduced version.", MessageType.Warning);
        }

        private void DrawPreview(MapData data, float unitsPerMeter)
        {
            if (GUILayout.Button("Preview"))
            {
                pendingData = data;
                pendingUnitsPerMeter = unitsPerMeter;
                EditorApplication.delayCall += RunPreview;
            }

            if (previewTexture == null)
            {
                return;
            }

            float width = Mathf.Min(MaxPreviewWidth, EditorGUIUtility.currentViewWidth - 40f);
            float height = width * previewTexture.height / previewTexture.width;
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(rect, previewTexture, ScaleMode.ScaleToFit);
        }

        private void RunPreview()
        {
            Texture2D result = executor.CapturePreview(pendingData, settings, pendingUnitsPerMeter);
            if (disposed)
            {
                DestroyTexture(result);
                return;
            }

            DestroyTexture(previewTexture);
            previewTexture = result;
            InternalEditorUtility.RepaintAllViews();
        }

        private void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }

        private void DrawCaptureButton(MapData data, float unitsPerMeter)
        {
            if (!GUILayout.Button("Capture"))
            {
                return;
            }

            if (data.Image != null)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Capture Map Image",
                    "This overwrites the current map image.",
                    "Capture",
                    "Cancel");
                if (!confirmed)
                {
                    return;
                }
            }

            pendingData = data;
            pendingUnitsPerMeter = unitsPerMeter;
            EditorApplication.delayCall += RunCapture;
        }

        private void RunCapture()
        {
            if (executor.Capture(pendingData, settings, pendingUnitsPerMeter))
            {
                SceneView.RepaintAll();
            }
            InternalEditorUtility.RepaintAllViews();
        }

        private void DrawChangeAreaButton(MapData data)
        {
            if (!data.Locked)
            {
                return;
            }

            if (!GUILayout.Button("Change area"))
            {
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Change Map Area",
                "This unlocks the rectangle and marks the captured image outdated. You will need to recapture.",
                "Change area",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            Undo.RecordObject(data, "Change Map Area");
            data.SetLocked(false);
            data.SetImageState(MapImageState.Outdated);
            EditorUtility.SetDirty(data);
        }

        private void DrawCustomImageField(MapData data)
        {
            EditorGUI.BeginChangeCheck();
            Texture2D newImage = (Texture2D)EditorGUILayout.ObjectField("Assign custom image", data.Image, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && newImage != null)
            {
                customImageAssigner.Assign(data, newImage);
            }

            if (data.Image != null)
            {
                DrawImportWarnings(data.Image);
            }
        }

        private void DrawImportWarnings(Texture2D image)
        {
            string assetPath = AssetDatabase.GetAssetPath(image);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            List<string> warnings = new List<string>();
            importSettings.GetWarnings(assetPath, warnings);
            for (int i = 0; i < warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
            }
        }

        private void DrawGuidance(MapData data)
        {
            ImageGuidance guidance = customImageAssigner.GetGuidance(data);
            EditorGUILayout.LabelField("Ratio", guidance.RatioText);
            DrawRecommendedSize("2048 px", guidance.Recommended2048);
            DrawRecommendedSize("4096 px", guidance.Recommended4096);
        }

        private void DrawRecommendedSize(string label, ImageSizePlan plan)
        {
            string text = plan.WidthPx + " x " + plan.HeightPx + " (" + plan.MetersPerPixel.ToString("0.###") + " m/px)";
            EditorGUILayout.LabelField(label, text);
        }

        private void DrawTemplateExport(MapData data)
        {
            templateSizeIndex = EditorGUILayout.Popup("Template Size", templateSizeIndex, templateSizeLabels);

            if (!GUILayout.Button("Export Template"))
            {
                return;
            }

            string path = templateExporter.GetTemplatePath(data);
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Export Template", "Save the map asset to disk first.", "OK");
                return;
            }
            if (data.RoadNetwork == null)
            {
                EditorUtility.DisplayDialog("Export Template", "Bake the road network first.", "OK");
                return;
            }

            ImageSizePlan plan = planner.PlanImageSize(data.RectangleSize, templateSizeValues[templateSizeIndex]);
            templateExporter.Export(data, data.RoadNetwork, plan, path);
            AssetDatabase.Refresh();
        }

        public void Dispose()
        {
            disposed = true;
            DestroyTexture(previewTexture);
            previewTexture = null;
        }
    }
}
