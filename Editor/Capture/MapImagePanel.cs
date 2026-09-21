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

        private readonly MapCaptureExecutor executor;
        private readonly NavigationEditorPrefs prefs;
        private readonly CaptureSettings settings;

        private MapData pendingData;
        private Texture2D previewTexture;
        private ICapturePipelineAdapter adapter;
        private float pendingUnitsPerMeter;
        private bool disposed;

        public MapImagePanel(NavigationEditorPrefs prefs)
        {
            this.prefs = prefs;
            executor = new MapCaptureExecutor();
            settings = new CaptureSettings();

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

        public void Dispose()
        {
            disposed = true;
            DestroyTexture(previewTexture);
            previewTexture = null;
        }
    }
}
