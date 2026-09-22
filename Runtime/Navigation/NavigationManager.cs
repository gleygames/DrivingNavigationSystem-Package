using System;
using System.Collections.Generic;
using Gley.Common;
using UnityEngine;

namespace Gley.NavigationSystem
{
    [DefaultExecutionOrder(-100)]
    public class NavigationManager : MonoBehaviour, INavigationCommandExecutor
    {
        private const float RotationTolerance = 0.01f;
        private const float MinHeadingLength = 0.0001f;

        private readonly List<NavigationMap> registeredMaps = new List<NavigationMap>();
        private readonly WorldConverter converter = new WorldConverter();
        private readonly VehicleMotion motion = new VehicleMotion();
        private readonly CommandQueue commandQueue = new CommandQueue();
        private readonly RoutePreferences preferences = new RoutePreferences();
        private readonly RouteRequest carRequest = new RouteRequest();
        private readonly RouteRequest generalRequest = new RouteRequest();

        [SerializeField] private NavigationSettings settings;
        [SerializeField] private NavigationFormatter formatter;
        [SerializeField] private NavigationMap explicitMap;
        [SerializeField] private Transform car;
        [SerializeField] private ShiftSource shiftSource = ShiftSource.Rectangle;
        [SerializeField] private RouteMode routeMode = RouteMode.Shortest;
        [SerializeField] private UTurnRule uTurnRule = UTurnRule.Never;
        private NavigationMap chosenMap;
        private MapData mapData;
        private RoadNetworkData roadNetwork;
        private OriginShiftTracker shiftTracker;
        private RoadQuery roadQuery;
        private Pathfinder pathfinder;
        private RoadMatcher matcher;
        private NavigationSession session;
        private RerouteDecider decider;
        private Route activeRoute;
        private Route previewRoute;
        private Route scratchRoute;
        private NavigationFormatter ownedFormatter;
        private Vector3 activeDestination;
        private Vector3 previewDestination;
        [SerializeField] private float carYawOffset;
        [SerializeField] private float avoidMultiplier = 5f;
        [SerializeField] private float preferMultiplier = 0.7f;
        [SerializeField] private float startSnapDistance = 200f;
        [SerializeField] private float destinationSnapDistance = 50f;
        [SerializeField] private float arrivalDistance = 10f;
        [SerializeField] private float turnedAroundDistance = 30f;
        [SerializeField] private float rerouteCooldown = 20f;
        [SerializeField] private float minHeadingSpeed = 1f;
        [SerializeField] private float stoppedSpeed = 0.1f;
        [SerializeField] private float teleportDistance = 50f;
        [SerializeField] private float leaveMargin = 3f;
        private float drivenDistance;
        private int dispatchDepth;
        [SerializeField] private bool startManually;
        private bool carNeedsReset;
        private bool severalMapsWarned;
        private bool rotationWarned;
        private bool hasActiveRoute;
        private bool hasPreview;
        private bool preferencesDirty;
        private bool processingQueue;
        private bool misconfigurationWarned;

        public event Action<NavigationMap> MapChanged;
        public event Action<Transform> CarChanged;
        public event Action<Route, MapMarker> PreviewReady;
        public event Action<FailureReason> PreviewFailed;
        public event Action PreviewCanceled;
        public event Action<Route> NavigationStarted;
        public event Action<Route, RerouteReason> Rerouted;
        public event Action<FailureReason> RouteFailed;
        public event Action Arrived;
        public event Action<StopReason> NavigationStopped;
        public event Action OffRoad;
        public event Action BackOnRoad;
        public event Action OutsideMap;
        public event Action BackInsideMap;

        public NavigationMap ActiveMap { get; private set; }
        public Transform Car { get { return car; } }
        public Route ActiveRoute
        {
            get
            {
                if (hasActiveRoute)
                {
                    return activeRoute;
                }
                return null;
            }
        }
        public Route PreviewRoute
        {
            get
            {
                if (hasPreview)
                {
                    return previewRoute;
                }
                return null;
            }
        }
        internal WorldConverter Converter { get { return converter; } }
        internal MapFrame Frame { get; private set; }
        internal RerouteReason LastRerouteDecision { get; private set; }
        public Vector3 NoseHeading { get { return motion.NoseHeading; } }
        internal Vector3 MovementHeading { get { return motion.MovementHeading; } }
        internal Vector3 CarTruePosition { get; private set; }
        internal Vector2 CarMapPosition { get; private set; }
        public float Speed { get { return motion.Speed; } }
        public float RemainingDistance
        {
            get
            {
                if (hasActiveRoute)
                {
                    return session.RemainingDistance;
                }
                return 0f;
            }
        }
        public float Eta
        {
            get
            {
                if (hasActiveRoute)
                {
                    return session.Eta;
                }
                return 0f;
            }
        }
        public float TrimDistance
        {
            get
            {
                if (hasActiveRoute)
                {
                    return session.TrimDistance;
                }
                return 0f;
            }
        }
        public NavigationFormatter Formatter { get { return formatter; } }
        public int CurrentRoadId { get; private set; } = -1;
        public bool IsInitialized { get; private set; }
        public bool IsOffRoad { get; private set; }
        public bool IsOutsideMap { get; private set; }
        public bool HasActiveRoute { get { return hasActiveRoute; } }
        public bool HasPreview { get { return hasPreview; } }
        internal bool CarTeleported { get { return motion.Teleported; } }

        private void Start()
        {
            if (!startManually)
            {
                Initialize();
            }
        }

        private void LateUpdate()
        {
            UpdateNavigationLogic(Time.deltaTime);
        }

        private void UpdateNavigationLogic(float deltaTime)
        {
            if (!IsInitialized)
            {
                return;
            }

            BeginDispatch();
            try
            {
                if (deltaTime > 0f)
                {
                    UpdateShiftLogic();
                    UpdateCarLogic(deltaTime);
                    UpdateTrackingLogic();
                    UpdateRouteLogic();
                    UpdatePreviewLogic();
                    UpdateOutsideMapLogic();
                }
                UpdatePreferencesLogic();
            }
            finally
            {
                EndDispatch();
            }
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
            IsInitialized = true;

            if (settings != null)
            {
                converter.SetUnitsPerMeter(settings.UnitsPerMeter);
            }
            else
            {
                CustomLogger.LogWarning("NavigationManager: no Navigation Settings assigned, using 1 unit per meter.", this);
            }

            EnsureFormatter();

            motion.TeleportDistance = teleportDistance;
            motion.StoppedSpeed = stoppedSpeed;
            motion.MinHeadingSpeed = minHeadingSpeed;

            preferences.Mode = routeMode;
            preferences.UTurn = uTurnRule;
            preferences.AvoidMultiplier = avoidMultiplier;
            preferences.PreferMultiplier = preferMultiplier;

            ApplySnapDistances(carRequest);
            ApplySnapDistances(generalRequest);

            EnsureShiftTracker();
            carNeedsReset = true;
            if (chosenMap == null)
            {
                chosenMap = explicitMap;
            }

            BeginDispatch();
            try
            {
                NavigationMap[] loadedMaps = FindObjectsByType<NavigationMap>(FindObjectsSortMode.None);
                for (int i = 0; i < loadedMaps.Length; i++)
                {
                    if (loadedMaps[i].isActiveAndEnabled)
                    {
                        AddRegisteredMap(loadedMaps[i]);
                    }
                }

                ApplyMapChoice(null);
            }
            finally
            {
                EndDispatch();
            }
        }

        private void EnsureFormatter()
        {
            if (formatter == null)
            {
                AssignDefaultFormatter();
            }
        }

        private void AssignDefaultFormatter()
        {
            DestroyOwnedFormatter();

            DefaultNavigationFormatter defaultFormatter = ScriptableObject.CreateInstance<DefaultNavigationFormatter>();
            defaultFormatter.SetSettings(settings);
            ownedFormatter = defaultFormatter;
            formatter = defaultFormatter;
        }

        private void DestroyOwnedFormatter()
        {
            if (ownedFormatter != null)
            {
                Destroy(ownedFormatter);
                ownedFormatter = null;
            }
        }

        public void OnOriginShifted(Vector3 delta)
        {
            if (shiftSource != ShiftSource.Manual)
            {
                CustomLogger.LogWarning("NavigationManager: OnOriginShifted only works when Shift source is Manual.", this);
                return;
            }

            EnsureShiftTracker();
            shiftTracker.AddManualDelta(delta);
            converter.SetShift(shiftTracker.Shift);
        }

        public void SetMap(NavigationMap map)
        {
            RunOrQueue(new NavigationCommand(NavigationCommandType.SetMap, map, null, Vector3.zero, 0f, 0, 0));
        }

        public void SetCar(Transform newCar, float yawOffset = 0f)
        {
            RunOrQueue(new NavigationCommand(NavigationCommandType.SetCar, null, newCar, Vector3.zero, yawOffset, 0, 0));
        }

        public void PreviewDestination(Vector3 worldPoint)
        {
            RunOrQueue(new NavigationCommand(NavigationCommandType.PreviewDestination, null, null, worldPoint, 0f, 0, 0));
        }

        public void StartNavigation(Vector3 worldPoint)
        {
            RunOrQueue(new NavigationCommand(NavigationCommandType.StartNavigation, null, null, worldPoint, 0f, 0, 0));
        }

        public void ConfirmPreview()
        {
            RunOrQueue(new NavigationCommand(NavigationCommandType.ConfirmPreview, null, null, Vector3.zero, 0f, 0, 0));
        }

        public void CancelPreview()
        {
            RunOrQueue(new NavigationCommand(NavigationCommandType.CancelPreview, null, null, Vector3.zero, 0f, 0, 0));
        }

        public void StopNavigation()
        {
            RunOrQueue(new NavigationCommand(NavigationCommandType.StopNavigation, null, null, Vector3.zero, 0f, 0, 0));
        }

        public void SetRouteMode(RouteMode mode)
        {
            RunOrQueue(new NavigationCommand(NavigationCommandType.SetRouteMode, null, null, Vector3.zero, 0f, 0, (int)mode));
        }

        public void SetRoadTypePreference(int typeId, RoadTypePreference preference)
        {
            RunOrQueue(new NavigationCommand(NavigationCommandType.SetRoadTypePreference, null, null, Vector3.zero, 0f, typeId, (int)preference));
        }

        public void SetUTurnRule(UTurnRule rule)
        {
            RunOrQueue(new NavigationCommand(NavigationCommandType.SetUTurnRule, null, null, Vector3.zero, 0f, 0, (int)rule));
        }

        public void RequestRoute(NavigationRouteRequest request, Action<Route> callback)
        {
            if (request == null)
            {
                CustomLogger.LogError("NavigationManager: RequestRoute was called with a null request.", this);
                return;
            }

            Route result = new Route();
            result.SetConverter(converter);

            if (pathfinder == null)
            {
                result.Clear();
                result.Failure = FailureReason.NoMap;
            }
            else
            {
                UpdateShiftLogic();
                generalRequest.Set(converter.WorldToTrue(request.From), converter.WorldToTrue(request.To));
                if (request.HasHeading)
                {
                    generalRequest.SetHeading(converter.WorldDirectionToTrue(request.Heading));
                }
                else
                {
                    generalRequest.ClearHeading();
                }

                if (request.Preferences != null)
                {
                    generalRequest.Preferences.CopyFrom(request.Preferences);
                }
                else
                {
                    generalRequest.Preferences.CopyFrom(preferences);
                }

                pathfinder.FindRoute(generalRequest, result);
            }

            if (callback != null)
            {
                callback(result);
            }
        }

        public void SetFormatter(NavigationFormatter value)
        {
            if (value == null)
            {
                AssignDefaultFormatter();
                return;
            }

            DestroyOwnedFormatter();
            formatter = value;
        }

        internal void RegisterMap(NavigationMap map)
        {
            if (!AddRegisteredMap(map))
            {
                return;
            }

            BeginDispatch();
            try
            {
                ApplyMapChoice(ActiveMap);
            }
            finally
            {
                EndDispatch();
            }
        }

        internal void UnregisterMap(NavigationMap map)
        {
            if (!registeredMaps.Remove(map))
            {
                return;
            }
            severalMapsWarned = false;

            BeginDispatch();
            try
            {
                NavigationMap previous = ActiveMap;
                if (map == chosenMap)
                {
                    chosenMap = null;
                }
                if (map == ActiveMap)
                {
                    DeactivateMap();
                }

                ApplyMapChoice(previous);
            }
            finally
            {
                EndDispatch();
            }
        }

        internal void SetSettings(NavigationSettings value)
        {
            settings = value;
        }

        internal void SetExplicitMap(NavigationMap value)
        {
            explicitMap = value;
        }

        internal void SetCarReference(Transform value, float yawOffset)
        {
            car = value;
            carYawOffset = yawOffset;
            carNeedsReset = true;
        }

        internal void SetStartManually(bool value)
        {
            startManually = value;
        }

        internal void SetShiftSource(ShiftSource value)
        {
            shiftSource = value;
        }

        void INavigationCommandExecutor.ExecuteNavigationCommand(NavigationCommand command)
        {
            RunCommand(command);
        }

        private void BeginDispatch()
        {
            dispatchDepth++;
        }

        private void UpdateShiftLogic()
        {
            EnsureShiftTracker();

            if (shiftTracker.Mode == ShiftSource.Rectangle)
            {
                if (ActiveMap == null)
                {
                    return;
                }
                shiftTracker.UpdateFromRectangle(ActiveMap.transform.position, mapData.EditTimeWorldPosition);
                CheckMapRotation();
            }

            converter.SetShift(shiftTracker.Shift);
        }

        private void EnsureShiftTracker()
        {
            if (shiftTracker != null && shiftTracker.Mode == shiftSource)
            {
                return;
            }
            shiftTracker = new OriginShiftTracker(shiftSource);
        }

        private void CheckMapRotation()
        {
            if (rotationWarned)
            {
                return;
            }

            float difference = Mathf.Abs(Mathf.DeltaAngle(ActiveMap.transform.eulerAngles.y, mapData.RectangleRotationY));
            if (difference > RotationTolerance)
            {
                rotationWarned = true;
                CustomLogger.LogWarning("The map object was rotated at runtime; floating origin systems should only move it.", ActiveMap);
            }
        }

        private void UpdateCarLogic(float deltaTime)
        {
            drivenDistance = 0f;
            if (car == null)
            {
                return;
            }

            Vector3 truePosition = converter.WorldToTrue(car.position);
            Vector3 trueNose = ReadTrueNose();

            if (carNeedsReset)
            {
                carNeedsReset = false;
                motion.Reset(truePosition, trueNose);
                if (matcher != null)
                {
                    matcher.Reset();
                }
            }

            motion.UpdateVehicleMotionLogic(truePosition, trueNose, deltaTime);
            CarTruePosition = truePosition;

            if (!motion.Teleported)
            {
                drivenDistance = motion.Speed * deltaTime;
            }
        }

        private Vector3 ReadTrueNose()
        {
            Vector3 worldNose = car.rotation * Quaternion.Euler(0f, carYawOffset, 0f) * Vector3.forward;
            return converter.WorldDirectionToTrue(worldNose);
        }

        private void UpdateTrackingLogic()
        {
            if (car == null || matcher == null)
            {
                IsOffRoad = false;
                CurrentRoadId = -1;
                return;
            }

            bool wasOffRoad = IsOffRoad;
            matcher.UpdateRoadMatchingLogic(CarTruePosition, motion.MovementHeading, motion.IsStopped, motion.Teleported);

            IsOffRoad = !matcher.IsOnRoad;
            if (matcher.IsOnRoad)
            {
                CurrentRoadId = roadNetwork.GetRoad(matcher.RoadIndex).Id;
            }
            else
            {
                CurrentRoadId = -1;
            }

            CheckMisconfigurationWarning(!wasOffRoad);

            if (IsOffRoad && !wasOffRoad)
            {
                RaiseOffRoad();
            }
            else if (!IsOffRoad && wasOffRoad)
            {
                RaiseBackOnRoad();
            }
        }

        private void RaiseOffRoad()
        {
            BeginDispatch();
            try
            {
                if (OffRoad != null)
                {
                    OffRoad();
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void EndDispatch()
        {
            dispatchDepth--;
            if (dispatchDepth > 0 || processingQueue)
            {
                return;
            }
            if (commandQueue.PendingCount == 0)
            {
                return;
            }

            processingQueue = true;
            try
            {
                commandQueue.Process(this);
            }
            finally
            {
                processingQueue = false;
            }
        }

        private void RaiseBackOnRoad()
        {
            BeginDispatch();
            try
            {
                if (BackOnRoad != null)
                {
                    BackOnRoad();
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void CheckMisconfigurationWarning(bool wasOnRoad)
        {
            if (misconfigurationWarned || !motion.Teleported || !wasOnRoad || roadQuery == null)
            {
                return;
            }

            RoadPoint nearestPoint;
            bool foundNearbyRoad = roadQuery.FindNearest(CarTruePosition, startSnapDistance, out nearestPoint);

            bool outsideMap = false;
            if (Frame != null)
            {
                outsideMap = !Frame.ContainsMap(Frame.TrueToMap(CarTruePosition));
            }

            if (foundNearbyRoad && !outsideMap)
            {
                return;
            }

            misconfigurationWarned = true;
            CustomLogger.LogWarning("The car jumped far away from all roads. If you use a floating origin system, make sure it moves the map object, or set Shift source to Manual.", this);
        }

        private void UpdateRouteLogic()
        {
            LastRerouteDecision = RerouteReason.None;
            if (car == null || session == null || !hasActiveRoute)
            {
                return;
            }

            decider.AccumulateDrivenDistance(drivenDistance);
            session.UpdateNavigationSessionLogic(matcher, drivenDistance);

            if (session.Arrived)
            {
                ClearActiveNavigation();
                RaiseArrived();
                return;
            }

            LastRerouteDecision = decider.Decide(session, matcher, motion.Teleported);
            if (LastRerouteDecision != RerouteReason.None)
            {
                RerouteFromCar(LastRerouteDecision);
                return;
            }

            if (motion.Teleported && matcher.IsOnRoad)
            {
                session.JumpTo(matcher);
            }
        }

        private void ClearActiveNavigation()
        {
            hasActiveRoute = false;
            if (session != null)
            {
                session.Stop();
            }
        }

        private void RaiseArrived()
        {
            BeginDispatch();
            try
            {
                if (Arrived != null)
                {
                    Arrived();
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void RerouteFromCar(RerouteReason reason)
        {
            FailureReason failure = FindCarRoute(activeDestination);
            if (failure != FailureReason.None)
            {
                ClearActiveNavigation();
                RaiseRouteFailed(failure);
                return;
            }

            decider.OnRerouted();

            if (scratchRoute.ArrivedImmediately)
            {
                ClearActiveNavigation();
                RaiseArrived();
                return;
            }

            SwapActiveRoute();
            session.Start(activeRoute);
            RaiseRerouted(activeRoute, reason);
        }

        private FailureReason FindCarRoute(Vector3 trueDestination)
        {
            if (pathfinder == null)
            {
                return FailureReason.NoMap;
            }
            if (car == null)
            {
                return FailureReason.NoCar;
            }

            UpdateShiftLogic();
            Vector3 truePosition = converter.WorldToTrue(car.position);
            carRequest.Set(truePosition, trueDestination);

            Vector3 heading = GetCarRequestHeading();
            if (heading.sqrMagnitude > MinHeadingLength)
            {
                carRequest.SetHeading(heading);
            }
            else
            {
                carRequest.ClearHeading();
            }

            carRequest.Preferences.CopyFrom(preferences);
            pathfinder.FindRoute(carRequest, scratchRoute);

            if (scratchRoute.Success)
            {
                return FailureReason.None;
            }
            if (scratchRoute.Failure == FailureReason.None)
            {
                return FailureReason.NoPath;
            }
            return scratchRoute.Failure;
        }

        private Vector3 GetCarRequestHeading()
        {
            Vector3 heading = motion.MovementHeading;
            if (carNeedsReset || heading.sqrMagnitude < MinHeadingLength)
            {
                heading = ReadTrueNose();
            }
            return new Vector3(heading.x, 0f, heading.z);
        }

        private void RaiseRouteFailed(FailureReason failure)
        {
            BeginDispatch();
            try
            {
                if (RouteFailed != null)
                {
                    RouteFailed(failure);
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void SwapActiveRoute()
        {
            Route previous = activeRoute;
            activeRoute = scratchRoute;
            scratchRoute = previous;
        }

        private void RaiseRerouted(Route route, RerouteReason reason)
        {
            BeginDispatch();
            try
            {
                if (Rerouted != null)
                {
                    Rerouted(route, reason);
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void UpdatePreviewLogic()
        {
            if (!hasPreview || car == null || matcher == null)
            {
                return;
            }
            if (!matcher.ChangedRoad && !matcher.EnteredRoad)
            {
                return;
            }

            RecomputePreview();
        }

        private void RecomputePreview()
        {
            FailureReason failure = FindCarRoute(previewDestination);
            if (failure != FailureReason.None)
            {
                RaisePreviewFailed(failure);
                return;
            }

            SwapPreviewRoute();
            RaisePreviewReady(previewRoute, null);
        }

        private void RaisePreviewFailed(FailureReason failure)
        {
            BeginDispatch();
            try
            {
                if (PreviewFailed != null)
                {
                    PreviewFailed(failure);
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void SwapPreviewRoute()
        {
            Route previous = previewRoute;
            previewRoute = scratchRoute;
            scratchRoute = previous;
        }

        private void RaisePreviewReady(Route route, MapMarker marker)
        {
            BeginDispatch();
            try
            {
                if (PreviewReady != null)
                {
                    PreviewReady(route, marker);
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void UpdateOutsideMapLogic()
        {
            if (car == null || Frame == null)
            {
                IsOutsideMap = false;
                return;
            }

            bool wasOutsideMap = IsOutsideMap;
            CarMapPosition = Frame.TrueToMap(CarTruePosition);
            IsOutsideMap = !Frame.ContainsMap(CarMapPosition);

            if (IsOutsideMap && !wasOutsideMap)
            {
                RaiseOutsideMap();
            }
            else if (!IsOutsideMap && wasOutsideMap)
            {
                RaiseBackInsideMap();
            }
        }

        private void RaiseOutsideMap()
        {
            BeginDispatch();
            try
            {
                if (OutsideMap != null)
                {
                    OutsideMap();
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void RaiseBackInsideMap()
        {
            BeginDispatch();
            try
            {
                if (BackInsideMap != null)
                {
                    BackInsideMap();
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void UpdatePreferencesLogic()
        {
            if (!preferencesDirty)
            {
                return;
            }
            preferencesDirty = false;

            if (hasActiveRoute)
            {
                RerouteFromCar(RerouteReason.PreferencesChanged);
            }
            if (hasPreview)
            {
                RecomputePreview();
            }
        }

        private void ApplySnapDistances(RouteRequest request)
        {
            request.StartSnapDistance = startSnapDistance;
            request.DestinationSnapDistance = destinationSnapDistance;
            request.ArrivalDistance = arrivalDistance;
        }

        private bool AddRegisteredMap(NavigationMap map)
        {
            if (map == null)
            {
                return false;
            }
            if (registeredMaps.Contains(map))
            {
                return false;
            }

            registeredMaps.Add(map);
            map.AttachManager(this);
            severalMapsWarned = false;
            return true;
        }

        private void ApplyMapChoice(NavigationMap previous)
        {
            if (!IsInitialized)
            {
                return;
            }

            SelectActiveMap();

            if (ActiveMap != previous)
            {
                RaiseMapChanged(ActiveMap);
            }
        }

        private void SelectActiveMap()
        {
            if (chosenMap != null)
            {
                if (chosenMap != ActiveMap)
                {
                    ActivateMap(chosenMap);
                }
                return;
            }

            if (ActiveMap != null)
            {
                return;
            }

            if (registeredMaps.Count == 1)
            {
                ActivateMap(registeredMaps[0]);
                return;
            }

            if (registeredMaps.Count > 1 && !severalMapsWarned)
            {
                severalMapsWarned = true;
                CustomLogger.LogWarning("NavigationManager: several maps loaded, call SetMap to choose the active one.", this);
            }
        }

        private void ActivateMap(NavigationMap map)
        {
            DeactivateMap();

            if (map.MapData == null)
            {
                CustomLogger.LogError("NavigationManager: the map object " + map.name + " has no Map asset assigned.", map);
                return;
            }

            ActiveMap = map;
            mapData = map.MapData;
            Frame = mapData.CreateFrame();
            rotationWarned = false;
            carNeedsReset = true;

            RoadNetworkData network = mapData.RoadNetwork;
            if (network == null || network.RoadCount == 0)
            {
                CustomLogger.LogWarning("NavigationManager: the map " + map.name + " has no baked road network. Navigation is not available on it.", map);
            }
            else
            {
                roadNetwork = network;
                roadQuery = new RoadQuery(network);
                pathfinder = new Pathfinder(network);

                matcher = new RoadMatcher(network);
                matcher.LeaveMargin = leaveMargin;

                session = new NavigationSession();
                session.ArrivalDistance = arrivalDistance;

                decider = new RerouteDecider();
                decider.RerouteCooldown = rerouteCooldown;
                decider.TurnedAroundDistance = turnedAroundDistance;

                activeRoute = CreateRoute();
                previewRoute = CreateRoute();
                scratchRoute = CreateRoute();
            }

            UpdateShiftLogic();
        }

        private void DeactivateMap()
        {
            if (hasActiveRoute)
            {
                ClearActiveNavigation();
                RaiseNavigationStopped(StopReason.MapChanged);
            }
            if (hasPreview)
            {
                hasPreview = false;
                RaisePreviewCanceled();
            }

            hasActiveRoute = false;
            hasPreview = false;
            ActiveMap = null;
            mapData = null;
            roadNetwork = null;
            Frame = null;
            roadQuery = null;
            pathfinder = null;
            matcher = null;
            session = null;
            decider = null;
            activeRoute = null;
            previewRoute = null;
            scratchRoute = null;
            LastRerouteDecision = RerouteReason.None;
            CurrentRoadId = -1;
            IsOffRoad = false;
            IsOutsideMap = false;
        }

        private void RaiseNavigationStopped(StopReason reason)
        {
            BeginDispatch();
            try
            {
                if (NavigationStopped != null)
                {
                    NavigationStopped(reason);
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void RaisePreviewCanceled()
        {
            BeginDispatch();
            try
            {
                if (PreviewCanceled != null)
                {
                    PreviewCanceled();
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private Route CreateRoute()
        {
            Route route = new Route();
            route.SetConverter(converter);
            return route;
        }

        private void RaiseMapChanged(NavigationMap map)
        {
            BeginDispatch();
            try
            {
                if (MapChanged != null)
                {
                    MapChanged(map);
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void RunOrQueue(NavigationCommand command)
        {
            if (dispatchDepth > 0)
            {
                commandQueue.Enqueue(command);
                return;
            }

            RunCommand(command);
        }

        private void RunCommand(NavigationCommand command)
        {
            BeginDispatch();
            try
            {
                ExecuteCommand(command);
            }
            finally
            {
                EndDispatch();
            }
        }

        private void ExecuteCommand(NavigationCommand command)
        {
            switch (command.Type)
            {
                case NavigationCommandType.SetMap:
                    ExecuteSetMap(command.Map);
                    break;
                case NavigationCommandType.SetCar:
                    ExecuteSetCar(command.Car, command.FloatValue);
                    break;
                case NavigationCommandType.PreviewDestination:
                    ExecutePreviewDestination(command.Point);
                    break;
                case NavigationCommandType.StartNavigation:
                    ExecuteStartNavigation(command.Point);
                    break;
                case NavigationCommandType.ConfirmPreview:
                    ExecuteConfirmPreview();
                    break;
                case NavigationCommandType.CancelPreview:
                    ExecuteCancelPreview();
                    break;
                case NavigationCommandType.StopNavigation:
                    ExecuteStopNavigation();
                    break;
                case NavigationCommandType.SetRouteMode:
                    ExecuteSetRouteMode((RouteMode)command.EnumValue);
                    break;
                case NavigationCommandType.SetRoadTypePreference:
                    ExecuteSetRoadTypePreference(command.IntValue, (RoadTypePreference)command.EnumValue);
                    break;
                case NavigationCommandType.SetUTurnRule:
                    ExecuteSetUTurnRule((UTurnRule)command.EnumValue);
                    break;
            }
        }

        private void ExecuteSetMap(NavigationMap map)
        {
            chosenMap = map;
            ApplyMapChoice(ActiveMap);
        }

        private void ExecuteSetCar(Transform newCar, float yawOffset)
        {
            car = newCar;
            carYawOffset = yawOffset;
            carNeedsReset = true;
            if (matcher != null)
            {
                matcher.Reset();
            }

            if (car == null)
            {
                if (hasActiveRoute)
                {
                    ClearActiveNavigation();
                    RaiseNavigationStopped(StopReason.CarRemoved);
                }
            }
            else
            {
                if (hasActiveRoute)
                {
                    RerouteFromCar(RerouteReason.CarChanged);
                }
                if (hasPreview)
                {
                    RecomputePreview();
                }
            }

            RaiseCarChanged(car);
        }

        private void RaiseCarChanged(Transform value)
        {
            BeginDispatch();
            try
            {
                if (CarChanged != null)
                {
                    CarChanged(value);
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void ExecutePreviewDestination(Vector3 worldPoint)
        {
            Vector3 trueDestination = WorldPointToTrue(worldPoint);
            FailureReason failure = FindCarRoute(trueDestination);
            if (failure != FailureReason.None)
            {
                hasPreview = false;
                RaisePreviewFailed(failure);
                return;
            }

            SwapPreviewRoute();
            previewDestination = trueDestination;
            hasPreview = true;
            RaisePreviewReady(previewRoute, null);
        }

        private Vector3 WorldPointToTrue(Vector3 worldPoint)
        {
            UpdateShiftLogic();
            return converter.WorldToTrue(worldPoint);
        }

        private void ExecuteStartNavigation(Vector3 worldPoint)
        {
            Vector3 trueDestination = WorldPointToTrue(worldPoint);
            FailureReason failure = FindCarRoute(trueDestination);
            if (failure != FailureReason.None)
            {
                RaiseRouteFailed(failure);
                return;
            }

            BeginNavigationFromScratch(trueDestination);
        }

        private void BeginNavigationFromScratch(Vector3 trueDestination)
        {
            if (scratchRoute.ArrivedImmediately)
            {
                ClearActiveNavigation();
                RaiseArrived();
                return;
            }

            SwapActiveRoute();
            activeDestination = trueDestination;
            hasActiveRoute = true;
            session.Start(activeRoute);
            RaiseNavigationStarted(activeRoute);
        }

        private void RaiseNavigationStarted(Route route)
        {
            BeginDispatch();
            try
            {
                if (NavigationStarted != null)
                {
                    NavigationStarted(route);
                }
            }
            finally
            {
                EndDispatch();
            }
        }

        private void ExecuteConfirmPreview()
        {
            if (!hasPreview)
            {
                return;
            }

            FailureReason failure = FindCarRoute(previewDestination);
            if (failure != FailureReason.None)
            {
                RaiseRouteFailed(failure);
                return;
            }

            hasPreview = false;
            BeginNavigationFromScratch(previewDestination);
        }

        private void ExecuteCancelPreview()
        {
            if (!hasPreview)
            {
                return;
            }

            hasPreview = false;
            RaisePreviewCanceled();
        }

        private void ExecuteStopNavigation()
        {
            if (!hasActiveRoute)
            {
                return;
            }

            ClearActiveNavigation();
            RaiseNavigationStopped(StopReason.StopCalled);
        }

        private void ExecuteSetRouteMode(RouteMode mode)
        {
            routeMode = mode;
            if (preferences.Mode == mode)
            {
                return;
            }

            preferences.Mode = mode;
            preferencesDirty = true;
        }

        private void ExecuteSetRoadTypePreference(int typeId, RoadTypePreference preference)
        {
            if (preferences.GetPreference(typeId) == preference)
            {
                return;
            }

            preferences.SetPreference(typeId, preference);
            preferencesDirty = true;
        }

        private void ExecuteSetUTurnRule(UTurnRule rule)
        {
            uTurnRule = rule;
            if (preferences.UTurn == rule)
            {
                return;
            }

            preferences.UTurn = rule;
            preferencesDirty = true;
        }

        private void OnDestroy()
        {
            commandQueue.Clear();
            DestroyOwnedFormatter();
        }
    }
}
