using UnityEngine;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public class FullMapInteractionSettings
    {
        [SerializeField] private FullMapZoomOutMode zoomOutMode = FullMapZoomOutMode.Fit;
        [SerializeField] private CrosshairMode crosshairMode = CrosshairMode.Auto;
        [SerializeField] private float openZoomMeters = 1000f;
        [SerializeField] private float mouseWheelStep = 1.25f;
        [SerializeField] private float doubleTapStep = 2f;
        [SerializeField] private float markerTapRadius = 40f;
        [SerializeField] private bool confirmStep = true;
        [SerializeField] private bool builtInPointerInput = true;
        [SerializeField] private bool fling = true;
        [SerializeField] private bool doubleTapZoom = true;

        public FullMapZoomOutMode ZoomOutMode { get { return zoomOutMode; } }
        public CrosshairMode CrosshairMode { get { return crosshairMode; } }
        public float OpenZoomMeters { get { return openZoomMeters; } }
        public float MouseWheelStep { get { return mouseWheelStep; } }
        public float DoubleTapStep { get { return doubleTapStep; } }
        public float MarkerTapRadius { get { return markerTapRadius; } }
        public bool ConfirmStep { get { return confirmStep; } }
        public bool BuiltInPointerInput { get { return builtInPointerInput; } }
        public bool Fling { get { return fling; } }
        public bool DoubleTapZoom { get { return doubleTapZoom; } }

        internal void SetZoomOutMode(FullMapZoomOutMode value)
        {
            zoomOutMode = value;
        }

        internal void SetCrosshairMode(CrosshairMode value)
        {
            crosshairMode = value;
        }

        internal void SetOpenZoomMeters(float value)
        {
            openZoomMeters = value;
        }

        internal void SetMouseWheelStep(float value)
        {
            mouseWheelStep = value;
        }

        internal void SetDoubleTapStep(float value)
        {
            doubleTapStep = value;
        }

        internal void SetMarkerTapRadius(float value)
        {
            markerTapRadius = value;
        }

        internal void SetConfirmStep(bool value)
        {
            confirmStep = value;
        }

        internal void SetBuiltInPointerInput(bool value)
        {
            builtInPointerInput = value;
        }

        internal void SetFling(bool value)
        {
            fling = value;
        }

        internal void SetDoubleTapZoom(bool value)
        {
            doubleTapZoom = value;
        }
    }
}
