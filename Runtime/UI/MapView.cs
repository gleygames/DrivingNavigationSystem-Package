using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(RectTransform))]
    public class MapView : MonoBehaviour
    {
        private const int MinimapChannelBit = 1 << 0;
        private const int FullMapChannelBit = 1 << 1;

        private readonly List<Vector2> emptyPoints = new List<Vector2>();
        private readonly List<float> emptyDistances = new List<float>();
        private readonly List<bool> emptyDashed = new List<bool>();
        private readonly MapViewMath math = new MapViewMath();

        [SerializeField] private NavigationManager manager;
        [SerializeField] private RectTransform viewport;
        private NavigationManager cachedManager;
        private RectTransform content;
        private Image backgroundImage;
        private RawImage mapImage;
        private RouteLineRenderer route;
        private MapFrame currentFrame;
        private Vector2 centerMap;
        [SerializeField] private float zoomMeters = 300f;
        [SerializeField] private float minZoomMeters = 50f;
        private float rotationDegrees;
        [SerializeField] private int channelMask = MinimapChannelBit | FullMapChannelBit;
        private bool hierarchyBuilt;

        public Vector2 CenterMap { get { return centerMap; } }
        internal Image BackgroundImage { get { return backgroundImage; } }
        internal RawImage MapImage { get { return mapImage; } }
        public float RotationDegrees { get { return rotationDegrees; } }
        public float ZoomMeters { get { return zoomMeters; } }
        public float CanvasUnitsPerMeter { get { return math.ComputeScale(viewport.rect.width, zoomMeters); } }

        private void OnEnable()
        {
            BuildHierarchyIfNeeded();

            NavigationManager found = FindManager();
            if (found == null)
            {
                return;
            }

            cachedManager = found;
            found.MapChanged += HandleMapChanged;
            HandleMapChanged(found.ActiveMap);
        }

        private void LateUpdate()
        {
            UpdateMapViewVisuals(Time.unscaledDeltaTime);
        }

        public void UpdateMapViewVisuals(float deltaTime)
        {
            if (content == null)
            {
                return;
            }

            ApplyContainerPose();
        }

        public void SetCenter(Vector2 value)
        {
            centerMap = value;
        }

        public void SetRotation(float value)
        {
            rotationDegrees = value;
        }

        public void SetZoomMeters(float meters, float maxZoomMeters)
        {
            zoomMeters = Mathf.Clamp(meters, minZoomMeters, maxZoomMeters);
        }

        public Vector3 ScreenToWorld(Vector2 screenPoint)
        {
            if (currentFrame == null || cachedManager == null)
            {
                return Vector3.zero;
            }

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, GetEventCamera(), out localPoint);
            Vector2 mapPoint = ViewportToMap(localPoint);
            Vector3 truePos = currentFrame.MapToTrue(mapPoint, currentFrame.Center.y);
            return cachedManager.Converter.TrueToWorld(truePos);
        }

        public Vector2 WorldToScreen(Vector3 world)
        {
            if (currentFrame == null || cachedManager == null)
            {
                return Vector2.zero;
            }

            Vector3 truePos = cachedManager.Converter.WorldToTrue(world);
            Vector2 mapPoint = currentFrame.TrueToMap(truePos);
            Vector2 localPoint = MapToViewport(mapPoint);
            Vector3 worldPoint = viewport.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0f));
            return RectTransformUtility.WorldToScreenPoint(GetEventCamera(), worldPoint);
        }

        internal Vector2 MapToViewport(Vector2 map)
        {
            return math.MapToViewport(map, ComputePose());
        }

        internal Vector2 ViewportToMap(Vector2 local)
        {
            return math.ViewportToMap(local, ComputePose());
        }

        private void BuildHierarchyIfNeeded()
        {
            if (hierarchyBuilt)
            {
                return;
            }

            if (viewport == null)
            {
                viewport = GetComponent<RectTransform>();
            }

            CreateBackground();
            CreateContent();
            CreateMapImage();
            CreateRoute();
            CreateMarkers();
            hierarchyBuilt = true;
        }

        private void CreateBackground()
        {
            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform));
            backgroundObject.transform.SetParent(viewport, false);

            RectTransform rectTransform = backgroundObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.raycastTarget = true;
        }

        private void CreateContent()
        {
            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);

            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.zero;
            content.pivot = Vector2.zero;
        }

        private void CreateMapImage()
        {
            GameObject imageObject = new GameObject("MapImage", typeof(RectTransform));
            imageObject.transform.SetParent(content, false);

            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            mapImage = imageObject.AddComponent<RawImage>();
            mapImage.raycastTarget = false;
        }

        private void CreateRoute()
        {
            GameObject routeObject = new GameObject("Route", typeof(RectTransform));
            routeObject.transform.SetParent(content, false);

            RectTransform rectTransform = routeObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            route = routeObject.AddComponent<RouteLineRenderer>();
        }

        private void CreateMarkers()
        {
            GameObject markersObject = new GameObject("Markers", typeof(RectTransform));
            markersObject.transform.SetParent(viewport, false);

            RectTransform rectTransform = markersObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private NavigationManager FindManager()
        {
            if (manager != null)
            {
                return manager;
            }
            if (cachedManager != null)
            {
                return cachedManager;
            }
            return FindAnyObjectByType<NavigationManager>();
        }

        private void HandleMapChanged(NavigationMap map)
        {
            if (map == null || map.MapData == null)
            {
                currentFrame = null;
                ClearRouteDisplay();
                return;
            }

            MapData data = map.MapData;
            mapImage.texture = data.Image;
            mapImage.rectTransform.sizeDelta = data.RectangleSize;
            backgroundImage.color = data.OutsideMapColor;
            currentFrame = data.CreateFrame();
            ClearRouteDisplay();
        }

        private void ClearRouteDisplay()
        {
            route.SetLine(emptyPoints, emptyDistances, emptyDashed);
        }

        private void ApplyContainerPose()
        {
            MapViewPose pose = ComputePose();
            content.localPosition = new Vector3(pose.Position.x, pose.Position.y, 0f);
            content.localRotation = Quaternion.Euler(0f, 0f, pose.RotationDegrees);
            content.localScale = new Vector3(pose.Scale, pose.Scale, 1f);
        }

        private MapViewPose ComputePose()
        {
            return math.ComputeContainerPose(centerMap, rotationDegrees, CanvasUnitsPerMeter, Vector2.zero);
        }

        private Camera GetEventCamera()
        {
            Canvas canvas = viewport.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            Canvas root = canvas.rootCanvas;
            if (root.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return root.worldCamera;
        }

        private void OnDisable()
        {
            if (cachedManager == null)
            {
                return;
            }

            cachedManager.MapChanged -= HandleMapChanged;
            cachedManager = null;
        }
    }
}
