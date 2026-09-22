using System;
using UnityEngine;
using UnityEngine.Events;

namespace Gley.NavigationSystem
{
    [Serializable]
    public class FailureReasonUnityEvent : UnityEvent<FailureReason>
    {
    }

    [Serializable]
    public class StopReasonUnityEvent : UnityEvent<StopReason>
    {
    }

    [Serializable]
    public class RerouteReasonUnityEvent : UnityEvent<RerouteReason>
    {
    }

    public class NavigationEvents : MonoBehaviour
    {
        [SerializeField] private NavigationManager manager;
        private NavigationManager cachedManager;

        [SerializeField] private UnityEvent onMapChanged = new UnityEvent();
        [SerializeField] private UnityEvent onCarChanged = new UnityEvent();
        [SerializeField] private UnityEvent onPreviewReady = new UnityEvent();
        [SerializeField] private FailureReasonUnityEvent onPreviewFailed = new FailureReasonUnityEvent();
        [SerializeField] private UnityEvent onPreviewCanceled = new UnityEvent();
        [SerializeField] private UnityEvent onNavigationStarted = new UnityEvent();
        [SerializeField] private RerouteReasonUnityEvent onRerouted = new RerouteReasonUnityEvent();
        [SerializeField] private FailureReasonUnityEvent onRouteFailed = new FailureReasonUnityEvent();
        [SerializeField] private UnityEvent onArrived = new UnityEvent();
        [SerializeField] private StopReasonUnityEvent onNavigationStopped = new StopReasonUnityEvent();
        [SerializeField] private UnityEvent onOffRoad = new UnityEvent();
        [SerializeField] private UnityEvent onBackOnRoad = new UnityEvent();
        [SerializeField] private UnityEvent onOutsideMap = new UnityEvent();
        [SerializeField] private UnityEvent onBackInsideMap = new UnityEvent();

        public UnityEvent MapChanged { get { return onMapChanged; } }
        public UnityEvent CarChanged { get { return onCarChanged; } }
        public UnityEvent PreviewReady { get { return onPreviewReady; } }
        public FailureReasonUnityEvent PreviewFailed { get { return onPreviewFailed; } }
        public UnityEvent PreviewCanceled { get { return onPreviewCanceled; } }
        public UnityEvent NavigationStarted { get { return onNavigationStarted; } }
        public RerouteReasonUnityEvent Rerouted { get { return onRerouted; } }
        public FailureReasonUnityEvent RouteFailed { get { return onRouteFailed; } }
        public UnityEvent Arrived { get { return onArrived; } }
        public StopReasonUnityEvent NavigationStopped { get { return onNavigationStopped; } }
        public UnityEvent OffRoad { get { return onOffRoad; } }
        public UnityEvent BackOnRoad { get { return onBackOnRoad; } }
        public UnityEvent OutsideMap { get { return onOutsideMap; } }
        public UnityEvent BackInsideMap { get { return onBackInsideMap; } }

        private void OnEnable()
        {
            NavigationManager found = FindManager();
            if (found == null)
            {
                return;
            }

            cachedManager = found;
            found.MapChanged += ForwardMapChanged;
            found.CarChanged += ForwardCarChanged;
            found.PreviewReady += ForwardPreviewReady;
            found.PreviewFailed += ForwardPreviewFailed;
            found.PreviewCanceled += ForwardPreviewCanceled;
            found.NavigationStarted += ForwardNavigationStarted;
            found.Rerouted += ForwardRerouted;
            found.RouteFailed += ForwardRouteFailed;
            found.Arrived += ForwardArrived;
            found.NavigationStopped += ForwardNavigationStopped;
            found.OffRoad += ForwardOffRoad;
            found.BackOnRoad += ForwardBackOnRoad;
            found.OutsideMap += ForwardOutsideMap;
            found.BackInsideMap += ForwardBackInsideMap;
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
            cachedManager = FindAnyObjectByType<NavigationManager>();
            return cachedManager;
        }

        private void ForwardMapChanged(NavigationMap map)
        {
            onMapChanged.Invoke();
        }

        private void ForwardCarChanged(Transform car)
        {
            onCarChanged.Invoke();
        }

        private void ForwardPreviewReady(Route route, MapMarker marker)
        {
            onPreviewReady.Invoke();
        }

        private void ForwardPreviewFailed(FailureReason reason)
        {
            onPreviewFailed.Invoke(reason);
        }

        private void ForwardPreviewCanceled()
        {
            onPreviewCanceled.Invoke();
        }

        private void ForwardNavigationStarted(Route route)
        {
            onNavigationStarted.Invoke();
        }

        private void ForwardRerouted(Route route, RerouteReason reason)
        {
            onRerouted.Invoke(reason);
        }

        private void ForwardRouteFailed(FailureReason reason)
        {
            onRouteFailed.Invoke(reason);
        }

        private void ForwardArrived()
        {
            onArrived.Invoke();
        }

        private void ForwardNavigationStopped(StopReason reason)
        {
            onNavigationStopped.Invoke(reason);
        }

        private void ForwardOffRoad()
        {
            onOffRoad.Invoke();
        }

        private void ForwardBackOnRoad()
        {
            onBackOnRoad.Invoke();
        }

        private void ForwardOutsideMap()
        {
            onOutsideMap.Invoke();
        }

        private void ForwardBackInsideMap()
        {
            onBackInsideMap.Invoke();
        }

        private void OnDisable()
        {
            if (cachedManager == null)
            {
                return;
            }

            cachedManager.MapChanged -= ForwardMapChanged;
            cachedManager.CarChanged -= ForwardCarChanged;
            cachedManager.PreviewReady -= ForwardPreviewReady;
            cachedManager.PreviewFailed -= ForwardPreviewFailed;
            cachedManager.PreviewCanceled -= ForwardPreviewCanceled;
            cachedManager.NavigationStarted -= ForwardNavigationStarted;
            cachedManager.Rerouted -= ForwardRerouted;
            cachedManager.RouteFailed -= ForwardRouteFailed;
            cachedManager.Arrived -= ForwardArrived;
            cachedManager.NavigationStopped -= ForwardNavigationStopped;
            cachedManager.OffRoad -= ForwardOffRoad;
            cachedManager.BackOnRoad -= ForwardBackOnRoad;
            cachedManager.OutsideMap -= ForwardOutsideMap;
            cachedManager.BackInsideMap -= ForwardBackInsideMap;
            cachedManager = null;
        }
    }
}
