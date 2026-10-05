using Unity.Profiling;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class MapViewFollowCar
    {
        private const float MinHeadingSqrMagnitude = 0.000001f;
        private const float TurnEndDegrees = 2f;

        private readonly MinimapMath math = new MinimapMath();
        private readonly ProfilerMarker followCarMarker = new ProfilerMarker("Gley.Nav.FollowCar");
        private readonly MapView view;
        private readonly MinimapFollowSettings settings;
        private readonly MinimapShapeSettings shapeSettings;

        private NavigationMap lastMap;
        private Transform lastCar;
        private float currentZoom;
        private float zoomVelocity;
        private float rotationVelocity;
        private float lastHeadingRotation;
        private float previousTargetRotation;
        private bool needsSnap = true;
        private bool inTurn;

        internal bool PinPlayerToEdge { get; private set; }

        internal MapViewFollowCar(MapView view, MinimapFollowSettings settings, MinimapShapeSettings shapeSettings)
        {
            this.view = view;
            this.settings = settings;
            this.shapeSettings = shapeSettings;
        }

        internal void UpdateFollowCarVisuals(float deltaTime)
        {
            using (followCarMarker.Auto())
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

                bool round = shapeSettings.Outline == EdgeShape.Circle;
                MapFrame frame = manager.Frame;
                UpdateRotation(manager, frame, deltaTime);
                UpdateZoom(manager, frame.Size, rect.size, round, deltaTime);
                UpdateCenter(manager, frame.Size, rect, round);
                PinPlayerToEdge = manager.IsOutsideMap;
                needsSnap = false;
            }
        }

        internal void Enable()
        {
            view.SetFollowCar(this);
            needsSnap = true;
        }

        private void UpdateRotation(NavigationManager manager, MapFrame frame, float deltaTime)
        {
            float target = 0f;
            if (settings.RotationMode == MinimapRotationMode.HeadingUp)
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
                float smoothTime = settings.RotationSmoothing;
                if (inTurn)
                {
                    smoothTime = settings.TurnSmoothing;
                }
                rotation = math.SmoothAngle(view.RotationDegrees, target, smoothTime, ref rotationVelocity, deltaTime);
            }

            previousTargetRotation = target;
            view.SetRotation(rotation);
        }

        private void UpdateTurnState(float target)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(previousTargetRotation, target)) > settings.TurnAngleThreshold)
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
            if (needsSnap || Mathf.Abs(Mathf.DeltaAngle(lastHeadingRotation, noseRotation)) > settings.NoseDeadZoneDegrees)
            {
                lastHeadingRotation = noseRotation;
            }
        }

        private void UpdateZoom(NavigationManager manager, Vector2 mapSize, Vector2 viewportSize, bool round, float deltaTime)
        {
            float maxZoom = math.MaxZoomToFitMap(mapSize, round, viewportSize);

            float target = settings.FixedZoomMeters;
            if (settings.SpeedZoom)
            {
                target = math.SpeedZoom(manager.Speed, settings.SpeedZoomMinMeters, settings.SpeedZoomMaxMeters, settings.SpeedZoomSlowSpeed, settings.SpeedZoomFastSpeed);
            }
            if (target > maxZoom)
            {
                target = maxZoom;
            }

            float zoomSmoothing = settings.ZoomSmoothing;
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

        private void UpdateCenter(NavigationManager manager, Vector2 mapSize, Rect rect, bool round)
        {
            float unitsPerMeter = view.CanvasUnitsPerMeter;
            float rotation = view.RotationDegrees;

            Vector2 carLocal = rect.center;
            if (settings.RotationMode == MinimapRotationMode.HeadingUp)
            {
                carLocal = new Vector2(rect.center.x, rect.yMin + settings.CarOffsetFromBottom * rect.height);
            }

            Vector2 carToViewCenter = math.Rotate((rect.center - carLocal) / unitsPerMeter, -rotation);
            Vector2 desiredViewCenter = manager.CarMapPosition + carToViewCenter;

            Vector2 viewHalfSizeMeters = rect.size * 0.5f / unitsPerMeter;
            float viewHalfExtentMeters = Mathf.Min(viewHalfSizeMeters.x, viewHalfSizeMeters.y);
            Vector2 clampedViewCenter = math.ClampCenter(desiredViewCenter, mapSize, viewHalfExtentMeters, round, rotation, viewHalfSizeMeters);

            Vector2 pivotToViewCenter = math.Rotate(rect.center / unitsPerMeter, -rotation);
            view.SetCenter(clampedViewCenter - pivotToViewCenter);
        }

        internal void Disable()
        {
            if (view.FollowCar == this)
            {
                view.SetFollowCar(null);
            }
        }
    }
}
