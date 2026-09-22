using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    public class MarkerLayer : MonoBehaviour
    {
        private readonly Dictionary<GameObject, List<GameObject>> pool = new Dictionary<GameObject, List<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> prefabOf = new Dictionary<GameObject, GameObject>();
        private readonly Dictionary<int, GameObject> activeInstances = new Dictionary<int, GameObject>();
        private readonly List<int> currentVisible = new List<int>();
        private readonly List<int> releaseScratch = new List<int>();
        private readonly HashSet<int> visibleSet = new HashSet<int>();
        private readonly OffScreenArrowMath edgeMath = new OffScreenArrowMath();

        private MapView view;
        private MapViewFollowCar followCar;
        [SerializeField] private GameObject defaultMarkerPrefab;
        [SerializeField] private float cullMarginFraction = 0.1f;

        public void UpdateMarkerLayerVisuals(float deltaTime)
        {
            EnsureFollowCarReference();

            if (view == null)
            {
                return;
            }

            NavigationManager manager = view.Manager;
            MapFrame frame = view.Frame;
            RectTransform viewport = view.Viewport;
            if (manager == null || frame == null || viewport == null)
            {
                ReleaseAllActive();
                return;
            }

            Rect viewportRect = viewport.rect;
            if (viewportRect.width <= 0f || viewportRect.height <= 0f)
            {
                return;
            }

            Vector2 minXZ;
            Vector2 maxXZ;
            ComputeVisibleTrueBounds(frame, viewportRect, out minXZ, out maxXZ);

            manager.Markers.QueryVisible(minXZ, maxXZ, view.ChannelMask, currentVisible);

            visibleSet.Clear();
            for (int i = 0; i < currentVisible.Count; i++)
            {
                visibleSet.Add(currentVisible[i]);
            }

            releaseScratch.Clear();
            foreach (KeyValuePair<int, GameObject> pair in activeInstances)
            {
                if (!visibleSet.Contains(pair.Key))
                {
                    releaseScratch.Add(pair.Key);
                }
            }
            for (int i = 0; i < releaseScratch.Count; i++)
            {
                ReleaseInstance(releaseScratch[i]);
            }

            for (int i = 0; i < currentVisible.Count; i++)
            {
                int index = currentVisible[i];
                MarkerEntry entry = manager.Markers.GetEntry(index);
                GameObject instance;
                if (!activeInstances.TryGetValue(index, out instance))
                {
                    instance = AcquireInstance(entry.Prefab);
                    if (instance == null)
                    {
                        continue;
                    }
                    activeInstances.Add(index, instance);
                }
                PositionMarker(instance, entry, frame);
            }
        }

        internal void SetView(MapView value)
        {
            view = value;
        }

        internal void SetDefaultMarkerPrefab(GameObject value)
        {
            defaultMarkerPrefab = value;
        }

        internal void SetCullMarginFraction(float value)
        {
            cullMarginFraction = value;
        }

        internal GameObject GetActiveInstance(int entryIndex)
        {
            GameObject instance;
            if (activeInstances.TryGetValue(entryIndex, out instance))
            {
                return instance;
            }
            return null;
        }

        private void EnsureFollowCarReference()
        {
            if (followCar != null || view == null)
            {
                return;
            }
            followCar = view.GetComponent<MapViewFollowCar>();
        }

        private void ReleaseAllActive()
        {
            releaseScratch.Clear();
            foreach (KeyValuePair<int, GameObject> pair in activeInstances)
            {
                releaseScratch.Add(pair.Key);
            }
            for (int i = 0; i < releaseScratch.Count; i++)
            {
                ReleaseInstance(releaseScratch[i]);
            }
        }

        private void ComputeVisibleTrueBounds(MapFrame frame, Rect viewportRect, out Vector2 minXZ, out Vector2 maxXZ)
        {
            Vector2 c0 = ViewportPointToTrueXZ(frame, new Vector2(viewportRect.xMin, viewportRect.yMin));
            Vector2 c1 = ViewportPointToTrueXZ(frame, new Vector2(viewportRect.xMin, viewportRect.yMax));
            Vector2 c2 = ViewportPointToTrueXZ(frame, new Vector2(viewportRect.xMax, viewportRect.yMin));
            Vector2 c3 = ViewportPointToTrueXZ(frame, new Vector2(viewportRect.xMax, viewportRect.yMax));

            float minX = Mathf.Min(Mathf.Min(c0.x, c1.x), Mathf.Min(c2.x, c3.x));
            float maxX = Mathf.Max(Mathf.Max(c0.x, c1.x), Mathf.Max(c2.x, c3.x));
            float minZ = Mathf.Min(Mathf.Min(c0.y, c1.y), Mathf.Min(c2.y, c3.y));
            float maxZ = Mathf.Max(Mathf.Max(c0.y, c1.y), Mathf.Max(c2.y, c3.y));

            float marginX = (maxX - minX) * cullMarginFraction;
            float marginZ = (maxZ - minZ) * cullMarginFraction;

            minXZ = new Vector2(minX - marginX, minZ - marginZ);
            maxXZ = new Vector2(maxX + marginX, maxZ + marginZ);
        }

        private Vector2 ViewportPointToTrueXZ(MapFrame frame, Vector2 viewportPoint)
        {
            Vector2 mapPoint = view.ViewportToMap(viewportPoint);
            Vector3 truePos = frame.MapToTrue(mapPoint, frame.Center.y);
            return new Vector2(truePos.x, truePos.z);
        }

        private void ReleaseInstance(int index)
        {
            GameObject instance;
            if (!activeInstances.TryGetValue(index, out instance))
            {
                return;
            }
            activeInstances.Remove(index);
            instance.SetActive(false);

            GameObject prefab;
            if (prefabOf.TryGetValue(instance, out prefab))
            {
                List<GameObject> free;
                if (!pool.TryGetValue(prefab, out free))
                {
                    free = new List<GameObject>();
                    pool.Add(prefab, free);
                }
                free.Add(instance);
            }
        }

        private GameObject AcquireInstance(GameObject prefab)
        {
            GameObject resolvedPrefab = prefab;
            if (resolvedPrefab == null)
            {
                resolvedPrefab = defaultMarkerPrefab;
            }
            if (resolvedPrefab == null)
            {
                return null;
            }

            List<GameObject> free;
            if (!pool.TryGetValue(resolvedPrefab, out free))
            {
                free = new List<GameObject>();
                pool.Add(resolvedPrefab, free);
            }

            if (free.Count > 0)
            {
                GameObject reused = free[free.Count - 1];
                free.RemoveAt(free.Count - 1);
                reused.SetActive(true);
                return reused;
            }

            GameObject created = Instantiate(resolvedPrefab, transform, false);
            RectTransform rect = created.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
            }
            prefabOf.Add(created, resolvedPrefab);
            return created;
        }

        private void PositionMarker(GameObject instance, MarkerEntry entry, MapFrame frame)
        {
            RectTransform rect = instance.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            Vector2 mapPoint = frame.TrueToMap(entry.TruePosition);
            Vector2 viewportPoint = view.MapToViewport(mapPoint);

            if (entry.IsPlayer && followCar != null && followCar.PinPlayerToEdge)
            {
                Vector2 edgePoint;
                float edgeAngle;
                Vector2 halfSize = view.Viewport.rect.size * 0.5f;
                if (edgeMath.ComputeEdgePoint(viewportPoint, halfSize, view.EdgeShape, view.EdgeInset, out edgePoint, out edgeAngle))
                {
                    viewportPoint = edgePoint;
                }
            }

            rect.anchoredPosition = viewportPoint;
            rect.localRotation = Quaternion.Euler(0f, 0f, ComputeRotationDegrees(entry, frame));
        }

        private float ComputeRotationDegrees(MarkerEntry entry, MapFrame frame)
        {
            if (entry.RotationMode == MarkerRotationMode.FollowMap)
            {
                return view.RotationDegrees;
            }
            if (entry.RotationMode == MarkerRotationMode.FollowHeading)
            {
                return view.RotationDegrees - frame.HeadingToMapAngle(entry.TrueHeading);
            }
            return 0f;
        }
    }
}
