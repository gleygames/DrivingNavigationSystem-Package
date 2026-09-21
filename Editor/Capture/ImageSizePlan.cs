using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public readonly struct ImageSizePlan
    {
        public Vector2 AdjustedRectSize { get; }
        public float MetersPerPixel { get; }
        public int WidthPx { get; }
        public int HeightPx { get; }

        public ImageSizePlan(int widthPx, int heightPx, float metersPerPixel, Vector2 adjustedRectSize)
        {
            WidthPx = widthPx;
            HeightPx = heightPx;
            MetersPerPixel = metersPerPixel;
            AdjustedRectSize = adjustedRectSize;
        }
    }
}
