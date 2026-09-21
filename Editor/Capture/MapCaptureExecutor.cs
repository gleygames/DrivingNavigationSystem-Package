using System;
using System.Collections.Generic;
using System.IO;
using Gley.Common;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class MapCaptureExecutor
    {
        public const int PreviewLongerSidePixels = 512;
        private const string CameraObjectName = "Navigation Capture Camera";
        private const string MapAssetSuffix = "_Map";
        private const string ImageFileSuffix = "_MapImage.png";

        private readonly CapturePlanner planner;
        private readonly MapImageImportSettings importSettings;

        public MapCaptureExecutor()
        {
            planner = new CapturePlanner();
            importSettings = new MapImageImportSettings();
        }

        public ICapturePipelineAdapter FindActiveAdapter()
        {
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<ICapturePipelineAdapter>();
            foreach (Type type in types)
            {
                if (type.IsAbstract || type.IsInterface || type == typeof(BuiltInCaptureAdapter))
                {
                    continue;
                }
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                ICapturePipelineAdapter adapter = (ICapturePipelineAdapter)Activator.CreateInstance(type);
                if (adapter.IsActive())
                {
                    return adapter;
                }
            }
            return new BuiltInCaptureAdapter();
        }

        public string GetImagePath(MapData data)
        {
            string assetPath = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            int lastSlash = assetPath.LastIndexOf('/');
            string folder = assetPath.Substring(0, lastSlash);
            string name = Path.GetFileNameWithoutExtension(assetPath);
            if (name.EndsWith(MapAssetSuffix))
            {
                name = name.Substring(0, name.Length - MapAssetSuffix.Length);
            }
            return folder + "/" + name + ImageFileSuffix;
        }

        public bool Capture(MapData data, CaptureSettings settings, float unitsPerMeter)
        {
            if (!CanCapture(data, unitsPerMeter))
            {
                return false;
            }

            string imagePath = GetImagePath(data);
            if (imagePath == null)
            {
                CustomLogger.LogError("MapCaptureExecutor: the map asset must be saved on disk before capturing.");
                return false;
            }

            ImageSizePlan plan = planner.PlanImageSize(data.RectangleSize, settings.LongerSidePixels);
            Color32[] pixels = RenderPixels(data, settings, unitsPerMeter, plan);

            bool isNewFile = !File.Exists(imagePath);
            SavePng(pixels, plan, imagePath);
            AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceUpdate);
            importSettings.ApplyIfNew(imagePath, isNewFile, Mathf.Max(plan.WidthPx, plan.HeightPx));

            Texture2D image = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            Color edgeColor = planner.AverageEdgeColor(pixels, plan.WidthPx, plan.HeightPx);
            UpdateMapData(data, plan, image, edgeColor);
            return true;
        }

        public Texture2D CapturePreview(MapData data, CaptureSettings settings, float unitsPerMeter)
        {
            if (!CanCapture(data, unitsPerMeter))
            {
                return null;
            }

            ImageSizePlan plan = planner.PlanImageSize(data.RectangleSize, PreviewLongerSidePixels);
            Color32[] pixels = RenderPixels(data, settings, unitsPerMeter, plan);

            Texture2D preview = new Texture2D(plan.WidthPx, plan.HeightPx, TextureFormat.RGB24, false);
            preview.hideFlags = HideFlags.HideAndDontSave;
            preview.wrapMode = TextureWrapMode.Clamp;
            preview.SetPixels32(pixels);
            preview.Apply(false);
            return preview;
        }

        private bool CanCapture(MapData data, float unitsPerMeter)
        {
            if (data == null)
            {
                CustomLogger.LogError("MapCaptureExecutor: no map data.");
                return false;
            }
            if (data.RectangleSize.x <= 0f || data.RectangleSize.y <= 0f)
            {
                CustomLogger.LogError("MapCaptureExecutor: the map rectangle has no area. Set its size first.");
                return false;
            }
            if (unitsPerMeter <= 0f)
            {
                CustomLogger.LogError("MapCaptureExecutor: unitsPerMeter must be greater than zero.");
                return false;
            }
            return true;
        }

        private Color32[] RenderPixels(MapData data, CaptureSettings settings, float unitsPerMeter, ImageSizePlan plan)
        {
            List<CapturePiece> pieces = planner.PlanPieces(plan, settings.PieceSizeMeters, settings.PieceOverlapPx);
            MapFrame frame = new MapFrame(data.RectangleCenter, plan.AdjustedRectSize, data.RectangleRotationY);
            CameraDepthPlan depth = PlanDepth(data, frame, settings.Layers, unitsPerMeter);
            Color32[] output = new Color32[plan.WidthPx * plan.HeightPx];

            int maxRenderWidth = 1;
            int maxRenderHeight = 1;
            for (int i = 0; i < pieces.Count; i++)
            {
                maxRenderWidth = Mathf.Max(maxRenderWidth, pieces[i].RenderWidthPx);
                maxRenderHeight = Mathf.Max(maxRenderHeight, pieces[i].RenderHeightPx);
            }

            ICapturePipelineAdapter adapter = FindActiveAdapter();
            bool fogBefore = RenderSettings.fog;
            RenderTexture activeBefore = RenderTexture.active;
            GameObject cameraObject = null;
            Texture2D pieceTexture = null;
            bool prepared = false;

            try
            {
                cameraObject = new GameObject(CameraObjectName);
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                Camera camera = cameraObject.AddComponent<Camera>();
                ConfigureCamera(camera, settings, frame, depth, unitsPerMeter);

                if (settings.DisableFog)
                {
                    RenderSettings.fog = false;
                }

                prepared = true;
                adapter.Prepare(camera, settings);

                pieceTexture = new Texture2D(maxRenderWidth, maxRenderHeight, TextureFormat.RGBA32, false, false);
                pieceTexture.hideFlags = HideFlags.HideAndDontSave;

                for (int i = 0; i < pieces.Count; i++)
                {
                    RenderPiece(camera, pieces[i], frame, plan, depth, unitsPerMeter, pieceTexture, output);
                }
            }
            finally
            {
                RenderTexture.active = activeBefore;
                if (prepared)
                {
                    adapter.Restore();
                }
                RenderSettings.fog = fogBefore;
                if (pieceTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(pieceTexture);
                }
                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }
            }

            return output;
        }

        private CameraDepthPlan PlanDepth(MapData data, MapFrame frame, LayerMask layers, float unitsPerMeter)
        {
            float rectMinX = float.MaxValue;
            float rectMaxX = float.MinValue;
            float rectMinZ = float.MaxValue;
            float rectMaxZ = float.MinValue;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 cornerTrue = frame.MapToTrue(GetCornerMap(corner, frame.Size), 0f);
                float worldX = cornerTrue.x * unitsPerMeter;
                float worldZ = cornerTrue.z * unitsPerMeter;
                rectMinX = Mathf.Min(rectMinX, worldX);
                rectMaxX = Mathf.Max(rectMaxX, worldX);
                rectMinZ = Mathf.Min(rectMinZ, worldZ);
                rectMaxZ = Mathf.Max(rectMaxZ, worldZ);
            }

            bool found = false;
            float minY = 0f;
            float maxY = 0f;
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }
                if ((layers.value & (1 << renderer.gameObject.layer)) == 0)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                if (bounds.max.x < rectMinX || bounds.min.x > rectMaxX || bounds.max.z < rectMinZ || bounds.min.z > rectMaxZ)
                {
                    continue;
                }

                if (!found)
                {
                    minY = bounds.min.y;
                    maxY = bounds.max.y;
                    found = true;
                }
                else
                {
                    minY = Mathf.Min(minY, bounds.min.y);
                    maxY = Mathf.Max(maxY, bounds.max.y);
                }
            }

            if (!found)
            {
                minY = data.RectangleCenter.y;
                maxY = data.RectangleCenter.y;
            }

            return planner.PlanCameraDepth(minY / unitsPerMeter, maxY / unitsPerMeter);
        }

        private Vector2 GetCornerMap(int corner, Vector2 size)
        {
            if (corner == 0)
            {
                return new Vector2(0f, 0f);
            }
            if (corner == 1)
            {
                return new Vector2(size.x, 0f);
            }
            if (corner == 2)
            {
                return new Vector2(size.x, size.y);
            }
            return new Vector2(0f, size.y);
        }

        private void ConfigureCamera(Camera camera, CaptureSettings settings, MapFrame frame, CameraDepthPlan depth, float unitsPerMeter)
        {
            if (settings.UseOwnCamera && settings.OwnCamera != null)
            {
                camera.CopyFrom(settings.OwnCamera);
            }

            camera.enabled = false;
            camera.targetTexture = null;
            camera.orthographic = true;
            camera.transform.rotation = Quaternion.Euler(90f, frame.RotationY, 0f);
            camera.nearClipPlane = depth.Near * unitsPerMeter;
            camera.farClipPlane = depth.Far * unitsPerMeter;
            camera.cullingMask = settings.Layers.value;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = settings.FillColor;
            camera.useOcclusionCulling = false;
        }

        private void RenderPiece(Camera camera, CapturePiece piece, MapFrame frame, ImageSizePlan plan, CameraDepthPlan depth, float unitsPerMeter, Texture2D pieceTexture, Color32[] output)
        {
            int width = piece.RenderWidthPx;
            int height = piece.RenderHeightPx;
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            try
            {
                float centerXMeters = (piece.RenderRect.x + piece.RenderRect.width * 0.5f) * plan.MetersPerPixel;
                float centerYMeters = (piece.RenderRect.y + piece.RenderRect.height * 0.5f) * plan.MetersPerPixel;
                Vector3 centerTrue = frame.MapToTrue(new Vector2(centerXMeters, centerYMeters), 0f);

                camera.transform.position = new Vector3(centerTrue.x * unitsPerMeter, depth.CameraY * unitsPerMeter, centerTrue.z * unitsPerMeter);
                camera.orthographicSize = piece.OrthoSize * unitsPerMeter;
                camera.targetTexture = renderTexture;
                camera.aspect = (float)width / height;
                camera.Render();

                RenderTexture.active = renderTexture;
                pieceTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                Color32[] piecePixels = pieceTexture.GetPixels32();

                CopyOutputRect(piece, pieceTexture.width, piecePixels, plan.WidthPx, output);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private void CopyOutputRect(CapturePiece piece, int pieceStride, Color32[] piecePixels, int imageWidth, Color32[] output)
        {
            RectInt outputRect = piece.OutputRect;
            RectInt renderRect = piece.RenderRect;

            for (int y = outputRect.y; y < outputRect.y + outputRect.height; y++)
            {
                int sourceRow = (y - renderRect.y) * pieceStride;
                int targetRow = y * imageWidth;
                for (int x = outputRect.x; x < outputRect.x + outputRect.width; x++)
                {
                    Color32 pixel = piecePixels[sourceRow + x - renderRect.x];
                    pixel.a = 255;
                    output[targetRow + x] = pixel;
                }
            }
        }

        private void SavePng(Color32[] pixels, ImageSizePlan plan, string imagePath)
        {
            Texture2D texture = new Texture2D(plan.WidthPx, plan.HeightPx, TextureFormat.RGB24, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false);
                byte[] png = texture.EncodeToPNG();
                File.WriteAllBytes(imagePath, png);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private void UpdateMapData(MapData data, ImageSizePlan plan, Texture2D image, Color edgeColor)
        {
            Undo.RecordObject(data, "Capture Map Image");
            data.SetRectangleSize(plan.AdjustedRectSize);
            data.SetImage(image);
            data.SetImageState(MapImageState.Captured);
            data.SetLocked(true);
            data.SetOutsideMapColor(edgeColor);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }
}
