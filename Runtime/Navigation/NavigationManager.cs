using System.Collections.Generic;
using Gley.Common;
using UnityEngine;

namespace Gley.NavigationSystem
{
    [DefaultExecutionOrder(-100)]
    public class NavigationManager : MonoBehaviour
    {
        private const float RotationTolerance = 0.01f;

        private readonly List<NavigationMap> registeredMaps = new List<NavigationMap>();
        private readonly WorldConverter converter = new WorldConverter();
        private readonly VehicleMotion motion = new VehicleMotion();

        [SerializeField] private NavigationSettings settings;
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
        private RoutePreferences preferences;
        private RouteRequest carRequest;
        private Route activeRoute;
        private Route previewRoute;
        private Route scratchRoute;
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
        [SerializeField] private bool startManually;
        private bool carNeedsReset;
        private bool severalMapsWarned;
        private bool rotationWarned;
        private bool hasActiveRoute;

        public NavigationMap ActiveMap { get; private set; }
        public Transform Car { get { return car; } }
        internal WorldConverter Converter { get { return converter; } }
        internal MapFrame Frame { get; private set; }
        internal RerouteReason LastRerouteDecision { get; private set; }
        public Vector3 NoseHeading { get { return motion.NoseHeading; } }
        internal Vector3 MovementHeading { get { return motion.MovementHeading; } }
        internal Vector3 CarTruePosition { get; private set; }
        internal Vector2 CarMapPosition { get; private set; }
        public float Speed { get { return motion.Speed; } }
        public int CurrentRoadId { get; private set; } = -1;
        public bool IsInitialized { get; private set; }
        public bool IsOffRoad { get; private set; }
        public bool IsOutsideMap { get; private set; }
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
            if (deltaTime <= 0f)
            {
                return;
            }

            UpdateShiftLogic();
            UpdateCarLogic(deltaTime);
            UpdateTrackingLogic();
            UpdateRouteLogic();
            UpdateOutsideMapLogic();
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

            motion.TeleportDistance = teleportDistance;
            motion.StoppedSpeed = stoppedSpeed;
            motion.MinHeadingSpeed = minHeadingSpeed;

            preferences = new RoutePreferences();
            preferences.Mode = routeMode;
            preferences.UTurn = uTurnRule;
            preferences.AvoidMultiplier = avoidMultiplier;
            preferences.PreferMultiplier = preferMultiplier;

            carRequest = new RouteRequest();
            carRequest.StartSnapDistance = startSnapDistance;
            carRequest.DestinationSnapDistance = destinationSnapDistance;
            carRequest.ArrivalDistance = arrivalDistance;

            EnsureShiftTracker();
            carNeedsReset = true;
            chosenMap = explicitMap;

            NavigationMap[] loadedMaps = FindObjectsByType<NavigationMap>(FindObjectsSortMode.None);
            for (int i = 0; i < loadedMaps.Length; i++)
            {
                if (loadedMaps[i].isActiveAndEnabled)
                {
                    AddRegisteredMap(loadedMaps[i]);
                }
            }

            ApplyMapChoice();
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

        internal void RegisterMap(NavigationMap map)
        {
            if (!AddRegisteredMap(map))
            {
                return;
            }
            ApplyMapChoice();
        }

        internal void UnregisterMap(NavigationMap map)
        {
            if (!registeredMaps.Remove(map))
            {
                return;
            }
            severalMapsWarned = false;

            if (map == chosenMap)
            {
                chosenMap = null;
            }
            if (map == ActiveMap)
            {
                DeactivateMap();
            }

            ApplyMapChoice();
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
            Vector3 worldNose = car.rotation * Quaternion.Euler(0f, carYawOffset, 0f) * Vector3.forward;
            Vector3 trueNose = converter.WorldDirectionToTrue(worldNose);

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

        private void UpdateTrackingLogic()
        {
            if (car == null || matcher == null)
            {
                IsOffRoad = false;
                CurrentRoadId = -1;
                return;
            }

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
            LastRerouteDecision = decider.Decide(session, matcher, motion.Teleported);
        }

        private void UpdateOutsideMapLogic()
        {
            if (car == null || Frame == null)
            {
                IsOutsideMap = false;
                return;
            }

            CarMapPosition = Frame.TrueToMap(CarTruePosition);
            IsOutsideMap = !Frame.ContainsMap(CarMapPosition);
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

        private void ApplyMapChoice()
        {
            if (!IsInitialized)
            {
                return;
            }

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
            if (session != null)
            {
                session.Stop();
            }

            hasActiveRoute = false;
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

        private Route CreateRoute()
        {
            Route route = new Route();
            route.SetConverter(converter);
            return route;
        }
    }
}
