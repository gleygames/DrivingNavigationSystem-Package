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
        private const int TrimModeRemove = 0;
        private const int TrimModeFade = 1;

        private readonly List<Vector2> emptyPoints = new List<Vector2>();
        private readonly List<float> emptyDistances = new List<float>();
        private readonly List<bool> emptyDashed = new List<bool>();
        private readonly List<Vector2> linePoints = new List<Vector2>();
        private readonly List<float> lineDistances = new List<float>();
        private readonly List<bool> lineDashed = new List<bool>();
        private readonly MapViewMath math = new MapViewMath();
        private readonly RouteLineData routeLineData = new RouteLineData();

        [SerializeField] private NavigationManager manager;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RouteStyle routeStyle;
        [SerializeField] private EdgeShape edgeShape = EdgeShape.Rectangle;
        [SerializeField] private GameObject arrowPrefab;
        private NavigationManager cachedManager;
        private RectTransform content;
        private Image backgroundImage;
        private RawImage mapImage;
        private RouteLineRenderer activeRouteRenderer;
        private RouteLineRenderer previewRouteRenderer;
        private MarkerLayer markerLayer;
        private MapFrame currentFrame;
        private Vector2 centerMap;
        [SerializeField] private float zoomMeters = 300f;
        [SerializeField] private float minZoomMeters = 50f;
        [SerializeField] private float edgeInset = 8f;
        private float rotationDegrees;
        [SerializeField] private int channelMask = MinimapChannelBit | FullMapChannelBit;
        [SerializeField] private bool showPreview = true;
        [SerializeField] private bool showOffScreenArrows = true;
        [SerializeField] private bool showArrowDistance = true;
        private bool hierarchyBuilt;

        public Vector2 CenterMap { get { return centerMap; } }
        internal Image BackgroundImage { get { return backgroundImage; } }
        internal RawImage MapImage { get { return mapImage; } }
        internal RouteLineRenderer ActiveRouteRenderer { get { return activeRouteRenderer; } }
        internal RouteLineRenderer PreviewRouteRenderer { get { return previewRouteRenderer; } }
        internal MarkerLayer MarkerLayer { get { return markerLayer; } }
        internal NavigationManager Manager { get { return cachedManager; } }
        internal RectTransform Viewport { get { return viewport; } }
        internal MapFrame Frame { get { return currentFrame; } }
        public EdgeShape EdgeShape { get { return edgeShape; } }
        internal GameObject ArrowPrefab { get { return arrowPrefab; } }
        public float RotationDegrees { get { return rotationDegrees; } }
        public float ZoomMeters { get { return zoomMeters; } }
        public float EdgeInset { get { return edgeInset; } }
        public float CanvasUnitsPerMeter { get { return math.ComputeScale(viewport.rect.width, zoomMeters); } }
        public int ChannelMask { get { return channelMask; } }
        public bool ShowOffScreenArrows { get { return showOffScreenArrows; } }
        public bool ShowArrowDistance { get { return showArrowDistance; } }

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
            found.NavigationStarted += HandleNavigationStarted;
            found.Rerouted += HandleRerouted;
            found.Arrived += HandleArrived;
            found.NavigationStopped += HandleNavigationStopped;
            found.RouteFailed += HandleRouteFailed;
            found.PreviewReady += HandlePreviewReady;
            found.PreviewFailed += HandlePreviewFailed;
            found.PreviewCanceled += HandlePreviewCanceled;
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
            UpdateRouteLineProperties();
            if (markerLayer != null)
            {
                markerLayer.UpdateMarkerLayerVisuals(deltaTime);
            }
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

        internal void SetShowPreview(bool value)
        {
            showPreview = value;
        }

        internal void SetChannelMask(int value)
        {
            channelMask = value;
        }

        internal void SetEdgeShape(EdgeShape value)
        {
            edgeShape = value;
        }

        internal void SetEdgeInset(float value)
        {
            edgeInset = value;
        }

        internal void SetArrowPrefab(GameObject value)
        {
            arrowPrefab = value;
        }

        internal void SetShowOffScreenArrows(bool value)
        {
            showOffScreenArrows = value;
        }

        internal void SetShowArrowDistance(bool value)
        {
            showArrowDistance = value;
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
            CreateRouteRenderers();
            CreateMarkers();
            ApplyRouteStyle();
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

        private void CreateRouteRenderers()
        {
            activeRouteRenderer = CreateRouteRenderer("ActiveRoute");
            previewRouteRenderer = CreateRouteRenderer("PreviewRoute");
        }

        private RouteLineRenderer CreateRouteRenderer(string objectName)
        {
            GameObject routeObject = new GameObject(objectName, typeof(RectTransform));
            routeObject.transform.SetParent(content, false);

            RectTransform rectTransform = routeObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            return routeObject.AddComponent<RouteLineRenderer>();
        }

        private void ApplyRouteStyle()
        {
            if (routeStyle == null)
            {
                return;
            }

            int trimMode = TrimModeRemove;
            if (routeStyle.DrivenMode == RouteDrivenMode.Faded)
            {
                trimMode = TrimModeFade;
            }

            activeRouteRenderer.SetStyle(routeStyle.ActiveLineColor, routeStyle.ActiveOutlineColor, routeStyle.FadedColor, routeStyle.HalfWidth, routeStyle.OutlineWidth, routeStyle.DashLength, routeStyle.GapLength, trimMode);
            previewRouteRenderer.SetStyle(routeStyle.PreviewLineColor, routeStyle.PreviewOutlineColor, routeStyle.FadedColor, routeStyle.HalfWidth, routeStyle.OutlineWidth, routeStyle.DashLength, routeStyle.GapLength, TrimModeRemove);
            activeRouteRenderer.SetShader(routeStyle.LineShader);
            previewRouteRenderer.SetShader(routeStyle.LineShader);
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

            markerLayer = markersObject.AddComponent<MarkerLayer>();
            markerLayer.SetView(this);
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
                ClearActiveLine();
                ClearPreviewLine();
                return;
            }

            MapData data = map.MapData;
            mapImage.texture = data.Image;
            mapImage.rectTransform.sizeDelta = data.RectangleSize;
            backgroundImage.color = data.OutsideMapColor;
            currentFrame = data.CreateFrame();
            ClearActiveLine();
            ClearPreviewLine();
        }

        private void HandleNavigationStarted(Route route)
        {
            RebuildActiveLine(route);
            ClearPreviewLine();
        }

        private void HandleRerouted(Route route, RerouteReason reason)
        {
            RebuildActiveLine(route);
        }

        private void HandleArrived()
        {
            ClearActiveLine();
            ClearPreviewLine();
        }

        private void HandleNavigationStopped(StopReason reason)
        {
            ClearActiveLine();
        }

        private void HandleRouteFailed(FailureReason reason)
        {
            if (!cachedManager.HasActiveRoute)
            {
                ClearActiveLine();
            }
        }

        private void HandlePreviewReady(Route route, MapMarker marker)
        {
            if (!showPreview)
            {
                return;
            }

            RebuildPreviewLine(route);
        }

        private void HandlePreviewFailed(FailureReason reason)
        {
            ClearPreviewLine();
        }

        private void HandlePreviewCanceled()
        {
            ClearPreviewLine();
        }

        private void RebuildActiveLine(Route route)
        {
            if (currentFrame == null)
            {
                return;
            }

            routeLineData.Convert(route, currentFrame, linePoints, lineDistances, lineDashed);
            activeRouteRenderer.SetLine(linePoints, lineDistances, lineDashed);
        }

        private void RebuildPreviewLine(Route route)
        {
            if (currentFrame == null)
            {
                return;
            }

            routeLineData.Convert(route, currentFrame, linePoints, lineDistances, lineDashed);
            previewRouteRenderer.SetLine(linePoints, lineDistances, lineDashed);
        }

        private void ClearActiveLine()
        {
            activeRouteRenderer.SetLine(emptyPoints, emptyDistances, emptyDashed);
        }

        private void ClearPreviewLine()
        {
            previewRouteRenderer.SetLine(emptyPoints, emptyDistances, emptyDashed);
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

        private void UpdateRouteLineProperties()
        {
            float unitsPerMeter = CanvasUnitsPerMeter;
            activeRouteRenderer.SetCanvasUnitsPerMeter(unitsPerMeter);
            previewRouteRenderer.SetCanvasUnitsPerMeter(unitsPerMeter);

            if (cachedManager != null)
            {
                activeRouteRenderer.SetTrimDistance(cachedManager.TrimDistance);
            }
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
            cachedManager.NavigationStarted -= HandleNavigationStarted;
            cachedManager.Rerouted -= HandleRerouted;
            cachedManager.Arrived -= HandleArrived;
            cachedManager.NavigationStopped -= HandleNavigationStopped;
            cachedManager.RouteFailed -= HandleRouteFailed;
            cachedManager.PreviewReady -= HandlePreviewReady;
            cachedManager.PreviewFailed -= HandlePreviewFailed;
            cachedManager.PreviewCanceled -= HandlePreviewCanceled;
            cachedManager = null;
        }
    }
}
