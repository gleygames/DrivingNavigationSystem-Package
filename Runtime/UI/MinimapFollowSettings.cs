using UnityEngine;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public class MinimapFollowSettings
    {
        [SerializeField] private MinimapRotationMode rotationMode = MinimapRotationMode.HeadingUp;
        [SerializeField] private float carOffsetFromBottom = 0.3f;
        [SerializeField] private float rotationSmoothing = 0.25f;
        [SerializeField] private float turnSmoothing = 0.8f;
        [SerializeField] private float turnAngleThreshold = 20f;
        [SerializeField] private float noseDeadZoneDegrees = 3f;
        [SerializeField] private bool speedZoom = true;
        [SerializeField] private float speedZoomMinMeters = 150f;
        [SerializeField] private float speedZoomMaxMeters = 500f;
        [SerializeField] private float speedZoomSlowSpeed = 5.5556f;
        [SerializeField] private float speedZoomFastSpeed = 27.7778f;
        [SerializeField] private float zoomSmoothing = 0.5f;
        [SerializeField] private float fixedZoomMeters = 300f;

        public MinimapRotationMode RotationMode { get { return rotationMode; } }
        public float CarOffsetFromBottom { get { return carOffsetFromBottom; } }
        public float RotationSmoothing { get { return rotationSmoothing; } }
        public float TurnSmoothing { get { return turnSmoothing; } }
        public float TurnAngleThreshold { get { return turnAngleThreshold; } }
        public float NoseDeadZoneDegrees { get { return noseDeadZoneDegrees; } }
        public bool SpeedZoom { get { return speedZoom; } }
        public float SpeedZoomMinMeters { get { return speedZoomMinMeters; } }
        public float SpeedZoomMaxMeters { get { return speedZoomMaxMeters; } }
        public float SpeedZoomSlowSpeed { get { return speedZoomSlowSpeed; } }
        public float SpeedZoomFastSpeed { get { return speedZoomFastSpeed; } }
        public float ZoomSmoothing { get { return zoomSmoothing; } }
        public float FixedZoomMeters { get { return fixedZoomMeters; } }

        internal void SetRotationMode(MinimapRotationMode value)
        {
            rotationMode = value;
        }

        internal void ToggleRotationMode()
        {
            if (rotationMode == MinimapRotationMode.HeadingUp)
            {
                rotationMode = MinimapRotationMode.NorthUp;
            }
            else
            {
                rotationMode = MinimapRotationMode.HeadingUp;
            }
        }

        internal void SetCarOffsetFromBottom(float value)
        {
            carOffsetFromBottom = value;
        }

        internal void SetRotationSmoothing(float value)
        {
            rotationSmoothing = value;
        }

        internal void SetTurnSmoothing(float value)
        {
            turnSmoothing = value;
        }

        internal void SetTurnAngleThreshold(float value)
        {
            turnAngleThreshold = value;
        }

        internal void SetNoseDeadZoneDegrees(float value)
        {
            noseDeadZoneDegrees = value;
        }

        internal void SetSpeedZoom(bool value)
        {
            speedZoom = value;
        }

        internal void SetSpeedZoomMinMeters(float value)
        {
            speedZoomMinMeters = value;
        }

        internal void SetSpeedZoomMaxMeters(float value)
        {
            speedZoomMaxMeters = value;
        }

        internal void SetSpeedZoomSlowSpeed(float value)
        {
            speedZoomSlowSpeed = value;
        }

        internal void SetSpeedZoomFastSpeed(float value)
        {
            speedZoomFastSpeed = value;
        }

        internal void SetZoomSmoothing(float value)
        {
            zoomSmoothing = value;
        }

        internal void SetFixedZoomMeters(float value)
        {
            fixedZoomMeters = value;
        }
    }
}
