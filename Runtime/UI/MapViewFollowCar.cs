using UnityEngine;

namespace Gley.NavigationSystem
{
    [DefaultExecutionOrder(99)]
    [RequireComponent(typeof(MapView))]
    public class MapViewFollowCar : MonoBehaviour
    {
        private const float MinHeadingSqrMagnitude = 0.000001f;
        private const float TurnEndDegrees = 2f;

        private readonly MinimapMath math = new MinimapMath();

        private MapView view;
        private NavigationMap lastMap;
        private Transform lastCar;
        [SerializeField] private MinimapRotationMode rotationMode = MinimapRotationMode.HeadingUp;
        [SerializeField] private float carOffsetFromBottom = 0.3f;
        [SerializeField] private float rotationSmoothing = 0.25f;
        [SerializeField] private float turnSmoothing = 0.8f;
        [SerializeField] private float turnAngleThreshold = 20f;
        [SerializeField] private float noseDeadZoneDegrees = 3f;
        [SerializeField] private float speedZoomMinMeters = 150f;
        [SerializeField] private float speedZoomMaxMeters = 500f;
        [SerializeField] private float speedZoomSlowSpeed = 5.5556f;
        [SerializeField] private float speedZoomFastSpeed = 27.7778f;
        [SerializeField] private float zoomSmoothing = 0.5f;
        [SerializeField] private float fixedZoomMeters = 300f;
        private float currentZoom;
        private float zoomVelocity;
        private float rotationVelocity;
        private float lastHeadingRotation;
        private float previousTargetRotation;
        [SerializeField] private bool speedZoom = true;
        [SerializeField] private bool round = true;
        private bool needsSnap = true;
        private bool inTurn;

        public MinimapRotationMode RotationMode { get { return rotationMode; } }
        internal bool PinPlayerToEdge { get; private set; }

        private void OnEnable()
        {
            view = GetComponent<MapView>();
            needsSnap = true;
        }

        private void LateUpdate()
        {
            UpdateFollowCarVisuals(Time.unscaledDeltaTime);
        }

        public void UpdateFollowCarVisuals(float deltaTime)
        {
            if (view == null)
            {
                return;
            }

            NavigationManager manager = view.Manager;
            if (manager == null || manager.Frame == null || manager.Car == null || view.Viewport == null)
            {
                PinPlayerToEdge = false;
                return;
            }

            Rect rect = view.Viewport.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            if (manager.ActiveMap != lastMap || manager.Car != lastCar)
            {
                lastMap = manager.ActiveMap;
                lastCar = manager.Car;
                needsSnap = true;
            }

            MapFrame frame = manager.Frame;
            UpdateRotation(manager, frame, deltaTime);
            UpdateZoom(manager, frame.Size, rect.size, deltaTime);
            UpdateCenter(manager, frame.Size, rect);
            PinPlayerToEdge = manager.IsOutsideMap;
            needsSnap = false;
        }

        public void SetRotationMode(MinimapRotationMode value)
        {
            rotationMode = value;
        }

        public void ToggleRotationMode()
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

        internal void SetRotationSmoothing(float value)
        {
            rotationSmoothing = value;
        }

        internal void SetTurnSmoothing(float value)
        {
            turnSmoothing = value;
        }

        internal void SetNoseDeadZoneDegrees(float value)
        {
            noseDeadZoneDegrees = value;
        }

        internal void SetZoomSmoothing(float value)
        {
            zoomSmoothing = value;
        }

        internal void SetSpeedZoom(bool value)
        {
            speedZoom = value;
        }

        internal void SetFixedZoomMeters(float value)
        {
            fixedZoomMeters = value;
        }

        internal void SetRound(bool value)
        {
            round = value;
        }

        private void UpdateRotation(NavigationManager manager, MapFrame frame, float deltaTime)
        {
            float target = 0f;
            if (rotationMode == MinimapRotationMode.HeadingUp)
            {
                UpdateHeadingTarget(manager, frame);
                target = lastHeadingRotation;
            }

            float rotation;
            if (needsSnap)
            {
                rotationVelocity = 0f;
                inTurn = false;
                rotation = Mathf.DeltaAngle(0f, target);
            }
            else
            {
                UpdateTurnState(target);
                float smoothTime = rotationSmoothing;
                if (inTurn)
                {
                    smoothTime = turnSmoothing;
                }
                rotation = math.SmoothAngle(view.RotationDegrees, target, smoothTime, ref rotationVelocity, deltaTime);
            }

            previousTargetRotation = target;
            view.SetRotation(rotation);
        }

        private void UpdateTurnState(float target)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(previousTargetRotation, target)) > turnAngleThreshold)
            {
                inTurn = true;
                return;
            }

            if (inTurn && Mathf.Abs(Mathf.DeltaAngle(view.RotationDegrees, target)) < TurnEndDegrees)
            {
                inTurn = false;
            }
        }

        private void UpdateHeadingTarget(NavigationManager manager, MapFrame frame)
        {
            if (manager.HasRoadHeading)
            {
                lastHeadingRotation = frame.HeadingToMapAngle(manager.RoadHeading);
                return;
            }

            Vector3 nose = manager.NoseHeading;
            if (nose.sqrMagnitude <= MinHeadingSqrMagnitude)
            {
                return;
            }

            float noseRotation = frame.HeadingToMapAngle(nose);
            if (needsSnap || Mathf.Abs(Mathf.DeltaAngle(lastHeadingRotation, noseRotation)) > noseDeadZoneDegrees)
            {
                lastHeadingRotation = noseRotation;
            }
        }

        private void UpdateZoom(NavigationManager manager, Vector2 mapSize, Vector2 viewportSize, float deltaTime)
        {
            float maxZoom = math.MaxZoomToFitMap(mapSize, round, viewportSize);

            float target = fixedZoomMeters;
            if (speedZoom)
            {
                target = math.SpeedZoom(manager.Speed, speedZoomMinMeters, speedZoomMaxMeters, speedZoomSlowSpeed, speedZoomFastSpeed);
            }
            if (target > maxZoom)
            {
                target = maxZoom;
            }

            if (needsSnap || zoomSmoothing <= 0f)
            {
                currentZoom = target;
                zoomVelocity = 0f;
            }
            else
            {
                currentZoom = Mathf.SmoothDamp(currentZoom, target, ref zoomVelocity, zoomSmoothing, Mathf.Infinity, deltaTime);
            }

            view.SetZoomMeters(currentZoom, maxZoom);
        }

        private void UpdateCenter(NavigationManager manager, Vector2 mapSize, Rect rect)
        {
            float unitsPerMeter = view.CanvasUnitsPerMeter;
            float rotation = view.RotationDegrees;

            Vector2 carLocal = rect.center;
            if (rotationMode == MinimapRotationMode.HeadingUp)
            {
                carLocal = new Vector2(rect.center.x, rect.yMin + carOffsetFromBottom * rect.height);
            }

            Vector2 carToViewCenter = math.Rotate((rect.center - carLocal) / unitsPerMeter, -rotation);
            Vector2 desiredViewCenter = manager.CarMapPosition + carToViewCenter;

            Vector2 viewHalfSizeMeters = rect.size * 0.5f / unitsPerMeter;
            float viewHalfExtentMeters = Mathf.Min(viewHalfSizeMeters.x, viewHalfSizeMeters.y);
            Vector2 clampedViewCenter = math.ClampCenter(desiredViewCenter, mapSize, viewHalfExtentMeters, round, rotation, viewHalfSizeMeters);

            Vector2 pivotToViewCenter = math.Rotate(rect.center / unitsPerMeter, -rotation);
            view.SetCenter(clampedViewCenter - pivotToViewCenter);
        }
    }
}
