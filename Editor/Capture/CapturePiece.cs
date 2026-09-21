using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public readonly struct CapturePiece
    {
        public RectInt OutputRect { get; }
        public RectInt RenderRect { get; }
        public Vector2 CenterMap { get; }
        public float OrthoSize { get; }
        public int RenderWidthPx { get; }
        public int RenderHeightPx { get; }

        public CapturePiece(RectInt outputRect, RectInt renderRect, Vector2 centerMap, float orthoSize, int renderWidthPx, int renderHeightPx)
        {
            OutputRect = outputRect;
            RenderRect = renderRect;
            CenterMap = centerMap;
            OrthoSize = orthoSize;
            RenderWidthPx = renderWidthPx;
            RenderHeightPx = renderHeightPx;
        }
    }
}
