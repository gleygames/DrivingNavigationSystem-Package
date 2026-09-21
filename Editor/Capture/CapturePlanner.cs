using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class CapturePlanner
    {
        public ImageSizePlan PlanImageSize(Vector2 rectSizeMeters, int longerSidePixels)
        {
            int longerPx = RoundUpToMultipleOf4(longerSidePixels);

            bool xIsLonger = rectSizeMeters.x >= rectSizeMeters.y;
            float longerMeters;
            float shorterMeters;
            if (xIsLonger)
            {
                longerMeters = rectSizeMeters.x;
                shorterMeters = rectSizeMeters.y;
            }
            else
            {
                longerMeters = rectSizeMeters.y;
                shorterMeters = rectSizeMeters.x;
            }

            float metersPerPixel = longerMeters / longerPx;
            int shorterPx = RoundUpToMultipleOf4(shorterMeters / metersPerPixel);
            float adjustedShorterMeters = shorterPx * metersPerPixel;

            int widthPx;
            int heightPx;
            float adjustedWidthMeters;
            float adjustedHeightMeters;
            if (xIsLonger)
            {
                widthPx = longerPx;
                heightPx = shorterPx;
                adjustedWidthMeters = longerMeters;
                adjustedHeightMeters = adjustedShorterMeters;
            }
            else
            {
                widthPx = shorterPx;
                heightPx = longerPx;
                adjustedWidthMeters = adjustedShorterMeters;
                adjustedHeightMeters = longerMeters;
            }

            Vector2 adjustedRectSize = new Vector2(adjustedWidthMeters, adjustedHeightMeters);
            return new ImageSizePlan(widthPx, heightPx, metersPerPixel, adjustedRectSize);
        }

        private int RoundUpToMultipleOf4(float value)
        {
            return Mathf.CeilToInt(value / 4f) * 4;
        }

        public List<CapturePiece> PlanPieces(ImageSizePlan plan, float pieceSizeMeters, int overlapPx)
        {
            List<CapturePiece> pieces = new List<CapturePiece>();

            int maxPieceSizePx = 4096 - 2 * overlapPx;
            int pieceSizePx = Mathf.RoundToInt(pieceSizeMeters / plan.MetersPerPixel);
            if (pieceSizePx > maxPieceSizePx)
            {
                pieceSizePx = maxPieceSizePx;
            }
            if (pieceSizePx < 1)
            {
                pieceSizePx = 1;
            }

            int columns = Mathf.CeilToInt((float)plan.WidthPx / pieceSizePx);
            int rows = Mathf.CeilToInt((float)plan.HeightPx / pieceSizePx);
            if (columns < 1)
            {
                columns = 1;
            }
            if (rows < 1)
            {
                rows = 1;
            }

            for (int row = 0; row < rows; row++)
            {
                int outputY = row * pieceSizePx;
                int outputHeight = pieceSizePx;
                if (outputY + outputHeight > plan.HeightPx)
                {
                    outputHeight = plan.HeightPx - outputY;
                }

                for (int column = 0; column < columns; column++)
                {
                    int outputX = column * pieceSizePx;
                    int outputWidth = pieceSizePx;
                    if (outputX + outputWidth > plan.WidthPx)
                    {
                        outputWidth = plan.WidthPx - outputX;
                    }

                    pieces.Add(BuildPiece(plan, outputX, outputY, outputWidth, outputHeight, overlapPx));
                }
            }

            return pieces;
        }

        private CapturePiece BuildPiece(ImageSizePlan plan, int outputX, int outputY, int outputWidth, int outputHeight, int overlapPx)
        {
            RectInt outputRect = new RectInt(outputX, outputY, outputWidth, outputHeight);
            RectInt renderRect = ExpandAndClip(outputRect, overlapPx, plan.WidthPx, plan.HeightPx);

            float centerXMeters = (outputRect.x + outputRect.width * 0.5f) * plan.MetersPerPixel;
            float centerYMeters = (outputRect.y + outputRect.height * 0.5f) * plan.MetersPerPixel;
            Vector2 centerMap = new Vector2(centerXMeters, centerYMeters);

            float orthoSize = renderRect.height * plan.MetersPerPixel * 0.5f;

            return new CapturePiece(outputRect, renderRect, centerMap, orthoSize, renderRect.width, renderRect.height);
        }

        private RectInt ExpandAndClip(RectInt rect, int overlapPx, int imageWidth, int imageHeight)
        {
            int x = rect.x - overlapPx;
            int y = rect.y - overlapPx;
            int xMax = rect.x + rect.width + overlapPx;
            int yMax = rect.y + rect.height + overlapPx;

            if (x < 0)
            {
                x = 0;
            }
            if (y < 0)
            {
                y = 0;
            }
            if (xMax > imageWidth)
            {
                xMax = imageWidth;
            }
            if (yMax > imageHeight)
            {
                yMax = imageHeight;
            }

            return new RectInt(x, y, xMax - x, yMax - y);
        }

        public CameraDepthPlan PlanCameraDepth(float minY, float maxY)
        {
            float cameraY = maxY + 10f;
            float near = 0.1f;
            float far = cameraY - minY + 10f;
            return new CameraDepthPlan(cameraY, near, far);
        }

        public Color AverageEdgeColor(Color32[] pixels, int width, int height)
        {
            float r = 0f;
            float g = 0f;
            float b = 0f;
            float a = 0f;
            int count = 0;

            AccumulateRow(pixels, width, 0, ref r, ref g, ref b, ref a, ref count);
            if (height > 1)
            {
                AccumulateRow(pixels, width, height - 1, ref r, ref g, ref b, ref a, ref count);
            }

            for (int y = 1; y < height - 1; y++)
            {
                AccumulateColor(pixels[y * width], ref r, ref g, ref b, ref a);
                count++;
                if (width > 1)
                {
                    AccumulateColor(pixels[y * width + width - 1], ref r, ref g, ref b, ref a);
                    count++;
                }
            }

            if (count == 0)
            {
                return Color.clear;
            }

            return new Color(r / count, g / count, b / count, a / count);
        }

        private void AccumulateRow(Color32[] pixels, int width, int row, ref float r, ref float g, ref float b, ref float a, ref int count)
        {
            int rowStart = row * width;
            for (int x = 0; x < width; x++)
            {
                AccumulateColor(pixels[rowStart + x], ref r, ref g, ref b, ref a);
                count++;
            }
        }

        private void AccumulateColor(Color32 pixel, ref float r, ref float g, ref float b, ref float a)
        {
            r += pixel.r / 255f;
            g += pixel.g / 255f;
            b += pixel.b / 255f;
            a += pixel.a / 255f;
        }
    }
}
