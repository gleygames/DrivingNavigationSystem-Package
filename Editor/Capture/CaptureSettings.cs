using System;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    [Serializable]
    public class CaptureSettings
    {
        public const float DefaultHdrpExposure = 13f;
        public const int MaxLongerSidePixels = 16384;

        private Camera ownCamera;
        [SerializeField] private Color fillColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private LayerMask layers = ~0;
        [SerializeField] private float hdrpExposure = DefaultHdrpExposure;
        [SerializeField] private float pieceSizeMeters = 200f;
        [SerializeField] private int longerSidePixels = 2048;
        [SerializeField] private int pieceOverlapPx = 16;
        [SerializeField] private bool disableFog = true;
        [SerializeField] private bool disablePostEffects = true;
        [SerializeField] private bool useOwnCamera;
        [SerializeField] private bool hdrpExposureEdited;

        public Camera OwnCamera { get { return ownCamera; } set { ownCamera = value; } }
        public Color FillColor { get { return fillColor; } set { fillColor = value; } }
        public LayerMask Layers { get { return layers; } set { layers = value; } }
        public float HdrpExposure { get { return hdrpExposure; } set { hdrpExposure = value; } }
        public float PieceSizeMeters { get { return pieceSizeMeters; } set { pieceSizeMeters = value; } }
        public int LongerSidePixels { get { return longerSidePixels; } set { longerSidePixels = value; } }
        public int PieceOverlapPx { get { return pieceOverlapPx; } set { pieceOverlapPx = value; } }
        public bool DisableFog { get { return disableFog; } set { disableFog = value; } }
        public bool DisablePostEffects { get { return disablePostEffects; } set { disablePostEffects = value; } }
        public bool UseOwnCamera { get { return useOwnCamera; } set { useOwnCamera = value; } }
        public bool HdrpExposureEdited { get { return hdrpExposureEdited; } set { hdrpExposureEdited = value; } }
    }
}
