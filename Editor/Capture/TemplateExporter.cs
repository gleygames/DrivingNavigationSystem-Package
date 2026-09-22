using System.IO;
using Gley.Common;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class TemplateExporter
    {
        private const string MapAssetSuffix = "_Map";
        private const string TemplateFileSuffix = "_MapTemplate.png";
        private const int LineHalfWidthPx = 1;

        private readonly Color32 backgroundColor;
        private readonly Color32 roadColor;

        public TemplateExporter()
        {
            backgroundColor = new Color32(128, 128, 128, 255);
            roadColor = new Color32(40, 40, 40, 255);
        }

        public string GetTemplatePath(MapData data)
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
            return folder + "/" + name + TemplateFileSuffix;
        }

        public void Export(MapData data, RoadNetworkData roads, ImageSizePlan size, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                CustomLogger.LogError("TemplateExporter: the map asset must be saved on disk before exporting a template.");
                return;
            }

            MapFrame frame = new MapFrame(data.RectangleCenter, size.AdjustedRectSize, data.RectangleRotationY);
            Color32[] pixels = BuildBackground(data.Image, size);
            DrawRoads(roads, frame, size, pixels);

            Texture2D texture = new Texture2D(size.WidthPx, size.HeightPx, TextureFormat.RGB24, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        internal Vector2Int MapToPixel(Vector2 map, ImageSizePlan size)
        {
            int x = Mathf.RoundToInt(map.x / size.AdjustedRectSize.x * (size.WidthPx - 1));
            int y = Mathf.RoundToInt(map.y / size.AdjustedRectSize.y * (size.HeightPx - 1));
            return new Vector2Int(x, y);
        }

        private Color32[] BuildBackground(Texture2D image, ImageSizePlan size)
        {
            Color32[] pixels = new Color32[size.WidthPx * size.HeightPx];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;
            }

            if (image == null)
            {
                return pixels;
            }

            Texture2D readable = LoadReadableCopy(image);
            if (readable == null)
            {
                return pixels;
            }

            try
            {
                DrawScaledToFit(readable, size, pixels);
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }

            return pixels;
        }

        private Texture2D LoadReadableCopy(Texture2D image)
        {
            string assetPath = AssetDatabase.GetAssetPath(image);
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
            {
                return null;
            }

            byte[] bytes = File.ReadAllBytes(assetPath);
            Texture2D readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            readable.hideFlags = HideFlags.HideAndDontSave;
            if (!readable.LoadImage(bytes))
            {
                Object.DestroyImmediate(readable);
                return null;
            }
            return readable;
        }

        private void DrawScaledToFit(Texture2D source, ImageSizePlan size, Color32[] pixels)
        {
            float scaleX = (float)size.WidthPx / source.width;
            float scaleY = (float)size.HeightPx / source.height;
            float scale = Mathf.Min(scaleX, scaleY);

            int scaledWidth = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int scaledHeight = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            int offsetX = (size.WidthPx - scaledWidth) / 2;
            int offsetY = (size.HeightPx - scaledHeight) / 2;

            Color32[] sourcePixels = source.GetPixels32();

            for (int y = 0; y < scaledHeight; y++)
            {
                int sourceY = Mathf.Clamp(Mathf.FloorToInt(y / scale), 0, source.height - 1);
                int destY = offsetY + y;
                if (destY < 0 || destY >= size.HeightPx)
                {
                    continue;
                }

                for (int x = 0; x < scaledWidth; x++)
                {
                    int sourceX = Mathf.Clamp(Mathf.FloorToInt(x / scale), 0, source.width - 1);
                    int destX = offsetX + x;
                    if (destX < 0 || destX >= size.WidthPx)
                    {
                        continue;
                    }

                    pixels[destY * size.WidthPx + destX] = sourcePixels[sourceY * source.width + sourceX];
                }
            }
        }

        private void DrawRoads(RoadNetworkData roads, MapFrame frame, ImageSizePlan size, Color32[] pixels)
        {
            for (int roadIndex = 0; roadIndex < roads.RoadCount; roadIndex++)
            {
                RoadRecord road = roads.GetRoad(roadIndex);
                for (int point = 0; point < road.PointCount - 1; point++)
                {
                    Vector3 trueStart = roads.GetPoint(road.FirstPoint + point);
                    Vector3 trueEnd = roads.GetPoint(road.FirstPoint + point + 1);
                    Vector2Int pixelStart = MapToPixel(frame.TrueToMap(trueStart), size);
                    Vector2Int pixelEnd = MapToPixel(frame.TrueToMap(trueEnd), size);
                    DrawThickLine(pixelStart, pixelEnd, size, pixels);
                }
            }
        }

        private void DrawThickLine(Vector2Int start, Vector2Int end, ImageSizePlan size, Color32[] pixels)
        {
            int steps = Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));
            if (steps < 1)
            {
                steps = 1;
            }

            for (int step = 0; step <= steps; step++)
            {
                float t = (float)step / steps;
                int centerX = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
                int centerY = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));
                DrawDot(centerX, centerY, size, pixels);
            }
        }

        private void DrawDot(int centerX, int centerY, ImageSizePlan size, Color32[] pixels)
        {
            for (int dy = -LineHalfWidthPx; dy <= LineHalfWidthPx; dy++)
            {
                int y = centerY + dy;
                if (y < 0 || y >= size.HeightPx)
                {
                    continue;
                }
                for (int dx = -LineHalfWidthPx; dx <= LineHalfWidthPx; dx++)
                {
                    int x = centerX + dx;
                    if (x < 0 || x >= size.WidthPx)
                    {
                        continue;
                    }
                    pixels[y * size.WidthPx + x] = roadColor;
                }
            }
        }
    }
}
