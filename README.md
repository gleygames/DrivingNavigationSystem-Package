# Driving Navigation System

Minimap and GPS navigation for driving games. Open the setup window from the Unity menu and follow steps 1-5.

Distances are in true meters, times in seconds, speeds in meters per second. "World" values are Unity world positions, "true" values are in real meters (world / `UnitsPerMeter`, plus any origin shift).

All runtime types live in the `Gley.NavigationSystem` namespace. The gamepad adapter lives in `Gley.NavigationSystem.InputSystem`.

## Quick start

```csharp
using Gley.NavigationSystem;

NavigationManager navigation = FindAnyObjectByType<NavigationManager>();

navigation.SetCar(car.transform, navigation.CarYawOffset);
navigation.NavigationStarted += route => Debug.Log(route.Length);
navigation.Arrived += () => Debug.Log("Arrived");

navigation.PreviewDestination(worldPoint);
navigation.ConfirmPreview();
```

Car spawned at runtime: turn on **Car spawned at runtime** in setup step 5, then call `SetCar` after the car exists. `NavigationManager.CarYawOffset` returns the yaw offset picked in the setup window.

All state-changing methods are safe to call from inside event callbacks. Calls made during event dispatch are queued and run afterwards.

## Index

| Area | Types |
| --- | --- |
| Main entry point | [NavigationManager](#navigationmanager), [NavigationEvents](#navigationevents), [NavigationRouteRequest](#navigationrouterequest) |
| Routes | [Route](#route), [RouteSegment](#routesegment), [RoutePreferences](#routepreferences), [Enums](#enums) |
| Markers | [MapMarker](#mapmarker) |
| Map data | [NavigationMap](#navigationmap), [MapData](#mapdata), [NavigationSettings](#navigationsettings), [RoadType](#roadtype) |
| UI | [MapView](#mapview), [MapViewFollowCar](#mapviewfollowcar), [MapViewInteractive](#mapviewinteractive), [MinimapTapToOpen](#minimaptaptoopen), [MinimapShape](#minimapshape), [Formatters and text](#formatters-and-text), [RouteStyle](#routestyle) |
| Input | [GamepadInputAdapter](#gamepadinputadapter) |
| Coordinates and units | [WorldConverter](#worldconverter), [MapFrame](#mapframe), [SpeedUnits](#speedunits) |
| Low level | [Advanced types](#advanced-types) |

---

## NavigationManager

`MonoBehaviour`. One per scene. Initializes itself in `Start` unless **Start Manually** is enabled, and updates in `LateUpdate`.

### Methods

| Method | Description |
| --- | --- |
| `void Initialize()` | Builds the pathfinder and marker registry for the active map. Called automatically unless Start Manually is on. |
| `void SetMap(NavigationMap map)` | Switches to another map. Stops any active navigation (`StopReason.MapChanged`). |
| `void SetCar(Transform newCar, float yawOffset = 0f)` | Sets the tracked car. `yawOffset` (degrees around Y) is for models whose forward is not +Z. Reroutes an active route and refreshes the preview. `null` stops navigation (`StopReason.CarRemoved`). |
| `void PreviewDestination(Vector3 worldPoint)` | Computes a route to the point without starting it. Raises `PreviewReady` or `PreviewFailed`. |
| `void ConfirmPreview()` | Starts navigating along the current preview. |
| `void CancelPreview()` | Discards the preview. Raises `PreviewCanceled`. |
| `void StartNavigation(Vector3 worldPoint)` | Computes a route and starts navigating immediately. Raises `NavigationStarted` or `RouteFailed`. |
| `void StopNavigation()` | Stops the active route. Raises `NavigationStopped` with `StopReason.StopCalled`. |
| `void SetRouteMode(RouteMode mode)` | Shortest or fastest routing. Reroutes an active route. |
| `void SetRoadTypePreference(int typeId, RoadTypePreference preference)` | Avoid, prefer or normal for a road type id (`RoadType.Id`). |
| `void SetUTurnRule(UTurnRule rule)` | Where U-turns are allowed in routes. |
| `void AddMarker(MapMarker marker)` | Registers a marker at runtime. |
| `void RemoveMarker(MapMarker marker)` | Unregisters a marker. |
| `void RequestRoute(NavigationRouteRequest request, Action<Route> callback)` | One-off route between two world points. Does not touch the active route, preview or events. The callback receives a `Route`, check `Route.Success`. |
| `void SetFormatter(NavigationFormatter value)` | Replaces the distance and duration text formatter. |
| `void OnOriginShifted(Vector3 delta)` | Call when your floating origin moves the world by `delta`. Only works when **Shift source** is Manual. |

### Properties

| Property | Type | Description |
| --- | --- | --- |
| `ActiveMap` | `NavigationMap` | The map in use. |
| `Car` | `Transform` | Current car. |
| `CarYawOffset` | `float` | Yaw offset in degrees, saved from the setup window or the last `SetCar`. |
| `ActiveRoute` | `Route` | Current route, or `null`. |
| `PreviewRoute` | `Route` | Current preview, or `null`. |
| `HasActiveRoute` | `bool` | A route is being followed. |
| `HasPreview` | `bool` | A preview exists. |
| `RemainingDistance` | `float` | Meters left on the active route, `0` with no route. |
| `Eta` | `float` | Seconds left on the active route, `0` with no route. |
| `TrimDistance` | `float` | Meters of the route already driven. |
| `Speed` | `float` | Car speed in meters per second. |
| `NoseHeading` | `Vector3` | Direction the car nose points, with yaw offset applied. |
| `CurrentRoadId` | `int` | Id of the road under the car, `-1` when none. |
| `IsInitialized` | `bool` | `Initialize` has run. |
| `IsOffRoad` | `bool` | Car is not on a road. |
| `IsOutsideMap` | `bool` | Car is outside the map rectangle. |
| `Formatter` | `NavigationFormatter` | Current formatter. |

### C# events

| Event | Signature | Raised when |
| --- | --- | --- |
| `MapChanged` | `Action<NavigationMap>` | The active map changed. |
| `CarChanged` | `Action<Transform>` | `SetCar` was applied. |
| `PreviewReady` | `Action<Route, MapMarker>` | A preview route is ready. The marker is the tapped marker, or `null`. |
| `PreviewFailed` | `Action<FailureReason>` | A preview could not be computed. |
| `PreviewCanceled` | `Action` | The preview was discarded. |
| `NavigationStarted` | `Action<Route>` | Navigation began. |
| `Rerouted` | `Action<Route, RerouteReason>` | A new route replaced the active one. |
| `RouteFailed` | `Action<FailureReason>` | Starting or rerouting failed. |
| `Arrived` | `Action` | The car reached the destination. |
| `NavigationStopped` | `Action<StopReason>` | Navigation ended without arriving. |
| `OffRoad` | `Action` | The car left the roads. |
| `BackOnRoad` | `Action` | The car returned to a road. |
| `OutsideMap` | `Action` | The car left the map rectangle. |
| `BackInsideMap` | `Action` | The car re-entered the map rectangle. |

## NavigationEvents

`MonoBehaviour`. Forwards every manager event to `UnityEvent`s so you can wire them in the Inspector. Finds the manager automatically.

| Property | Type |
| --- | --- |
| `MapChanged`, `CarChanged`, `PreviewReady`, `PreviewCanceled`, `NavigationStarted`, `Arrived`, `OffRoad`, `BackOnRoad`, `OutsideMap`, `BackInsideMap` | `UnityEvent` |
| `PreviewFailed`, `RouteFailed` | `FailureReasonUnityEvent` (`UnityEvent<FailureReason>`) |
| `NavigationStopped` | `StopReasonUnityEvent` (`UnityEvent<StopReason>`) |
| `Rerouted` | `RerouteReasonUnityEvent` (`UnityEvent<RerouteReason>`) |

## NavigationRouteRequest

Input for `NavigationManager.RequestRoute`.

| Member | Description |
| --- | --- |
| `NavigationRouteRequest()` | Empty request. |
| `NavigationRouteRequest(Vector3 from, Vector3 to)` | Request between two world points. |
| `Vector3 From`, `Vector3 To` | World start and destination. |
| `Vector3 Heading`, `bool HasHeading` | Optional travel direction at the start. |
| `RoutePreferences Preferences` | Mode, U-turn rule and road type preferences for this request. |

## Route

Result of a route computation.

| Member | Description |
| --- | --- |
| `bool Success` | The route was found. |
| `FailureReason Failure` | Why it failed, `None` on success. |
| `float Length` | Total meters. |
| `float Eta` | Total seconds. |
| `Vector3 Destination` | Destination position. |
| `bool ArrivedImmediately` | Start was already within arrival distance. |
| `RoadPoint Start`, `RoadPoint End` | Snapped road positions at each end. |
| `IReadOnlyList<RouteSegment> Segments` | Road pieces the route follows, in order. |
| `void GetPoints(List<Vector3> output)` | Route polyline as world positions. |
| `void GetTruePoints(List<Vector3> output)` | Route polyline in true meters. |
| `void Clear()` | Empties the route. |

## RouteSegment

Struct describing one road piece of a route.

| Property | Description |
| --- | --- |
| `int RoadIndex`, `int RoadId` | Road in the road network. |
| `bool Forward` | Driven along the road direction. |
| `float FromDistance`, `float ToDistance` | Distance along the route where the segment starts and ends. |

## RoutePreferences

| Member | Description |
| --- | --- |
| `RouteMode Mode` | Shortest or Fastest. |
| `UTurnRule UTurn` | U-turn rule. |
| `float AvoidMultiplier`, `float PreferMultiplier` | Cost multipliers for avoided or preferred road types. |
| `void SetPreference(int typeId, RoadTypePreference value)` | Set a road type preference. |
| `RoadTypePreference GetPreference(int typeId)` | Read a road type preference. |
| `float GetMultiplier(int typeId)` | Cost multiplier for a road type. |
| `void CopyFrom(RoutePreferences other)` | Copy all values. |

## Enums

| Enum | Values |
| --- | --- |
| `RouteMode` | `Shortest`, `Fastest` |
| `UTurnRule` | `Never`, `AtIntersections`, `Anywhere` |
| `RoadTypePreference` | `Normal`, `Avoid`, `Prefer` |
| `FailureReason` | `None`, `NoMap`, `NoCar`, `NoRoadNearStart`, `NoRoadNearDestination`, `NoPath` |
| `StopReason` | `StopCalled`, `MapChanged`, `CarRemoved` |
| `RerouteReason` | `None`, `WrongTurn`, `TurnedAround`, `BackOnRoad`, `CarChanged`, `PreferencesChanged`, `Teleported` |
| `MarkerRotationMode` | `Upright`, `FollowHeading`, `FollowMap` |
| `MinimapRotationMode` | `HeadingUp`, `NorthUp` |
| `MinimapShapeKind` | `Rectangle`, `Sprite` |
| `MinimapTapAction` | `OpenFullMap`, `Nothing` |
| `FullMapZoomOutMode` | `Fit`, `Fill` |
| `CrosshairMode` | `Auto`, `Always`, `Never` |
| `EdgeShape` | `Rectangle`, `Circle` |
| `RouteDrivenMode` | `Removed`, `Faded` |
| `NavigationUnitSystem` | `ProjectSetting`, `Metric`, `Imperial` |
| `MapImageState` | `None`, `Captured`, `Custom`, `Outdated` |

## MapMarker

`MonoBehaviour`. Put it on any scene object to show it on the minimap and full map. Configure it in the Inspector, or add it from code with `NavigationManager.AddMarker`.

| Property | Description |
| --- | --- |
| `GameObject Prefab` | UI prefab drawn for this marker. |
| `MarkerRotationMode RotationMode` | Upright, follow the object heading, or follow the map. |
| `int ChannelMask` | Which map views show it (bit per channel, see `NavigationSettings.ChannelCount`). |
| `bool IsStatic` | The object never moves, so its position is read once. |
| `bool CanBeDestination` | Tapping it on the full map previews a route to it. |
| `bool ShowOffScreenArrow` | Show an edge arrow when it is outside the view. |

## NavigationMap

`MonoBehaviour`. Scene object that places a `MapData` in the world.

| Property | Description |
| --- | --- |
| `MapData MapData` | The map asset. |

## MapData

`ScriptableObject`. The map rectangle, image and baked road network.

| Property | Description |
| --- | --- |
| `Vector3 RectangleCenter` | Map center in true meters. |
| `Vector2 RectangleSize` | Map size in meters. |
| `float RectangleRotationY` | Map rotation in degrees. |
| `Texture2D Image` | Map image. |
| `MapImageState ImageState` | How the image was produced. |
| `Color OutsideMapColor` | Color shown outside the map. |
| `RoadNetworkData RoadNetwork` | Baked road network. |
| `bool Locked` | Editing is locked. |
| `Vector3 EditTimeWorldPosition` | World position of the map object when it was authored. |
| `int FormatVersion` | Data format version. |
| `MapFrame CreateFrame()` | Coordinate frame for this map. |

## NavigationSettings

`ScriptableObject`. Project-wide settings, created by the setup window.

| Member | Description |
| --- | --- |
| `float UnitsPerMeter` | Unity units per real meter. |
| `bool ImperialUnits` | Display miles and feet. |
| `bool BlockBuildOnProblems` | Bake refuses to run with validation errors. |
| `IReadOnlyList<RoadType> RoadTypes` | All road types. |
| `const int ChannelCount` | Number of marker channels (8). |
| `RoadType AddRoadType(string name)` | Add a road type. |
| `RoadType FindRoadType(int id)` | Find by id. |
| `int GetRoadTypeIndex(int id)` | Index for an id. |
| `bool RemoveRoadType(int id)` | Remove by id. |
| `void MoveRoadType(int fromIndex, int toIndex)` | Reorder. |
| `void SetRoadTypeName / SetRoadTypeSpeed / SetRoadTypeWidth / SetRoadTypeColor(int id, value)` | Edit a road type. |
| `string GetChannelName(int index)`, `void SetChannelName(int index, string value)` | Marker channel names. |
| `void SetUnitsPerMeter(float)`, `void SetImperialUnits(bool)`, `void SetBlockBuildOnProblems(bool)` | Setters. |
| `void ResetToDefaults()` | Restore defaults. |

## RoadType

| Member | Description |
| --- | --- |
| `int Id` | Stable id used by `SetRoadTypePreference`. |
| `string Name` | Display name. |
| `float SpeedMetersPerSecond` | Speed used for ETA and fastest routing. |
| `float WidthMeters` | Road width. |
| `Color EditorColor` | Color in the road editor. |

## MapView

`MonoBehaviour`. Draws a window into the map. Used by both the minimap and the full map.

| Member | Description |
| --- | --- |
| `Vector2 CenterMap` | Center in map coordinates. |
| `float ZoomMeters` | Meters visible across the view width. |
| `float RotationDegrees` | View rotation. |
| `float CanvasUnitsPerMeter` | Canvas scale at the current zoom. |
| `int ChannelMask` | Marker channels drawn. |
| `EdgeShape EdgeShape`, `float EdgeInset` | Off-screen arrow placement. |
| `bool ShowOffScreenArrows`, `bool ShowArrowDistance` | Off-screen arrow options. |
| `void SetCenter(Vector2 value)` | Move the view. |
| `void SetRotation(float value)` | Rotate the view. |
| `void SetZoomMeters(float meters, float maxZoomMeters)` | Zoom, clamped to the maximum. |
| `Vector3 ScreenToWorld(Vector2 screenPoint)` | Screen position to world position. |
| `Vector2 WorldToScreen(Vector3 world)` | World position to screen position. |

## MapViewFollowCar

`MonoBehaviour`. Makes a `MapView` follow the car (minimap).

| Member | Description |
| --- | --- |
| `MinimapRotationMode RotationMode` | Heading up or north up. |
| `void SetRotationMode(MinimapRotationMode value)` | Set the mode. |
| `void ToggleRotationMode()` | Switch between the two modes. |

## MapViewInteractive

`MonoBehaviour`. Full map with pan, zoom, tap to preview and confirm.

| Member | Description |
| --- | --- |
| `event Action Opened`, `event Action Closed` | Full map shown or hidden. |
| `void Open()`, `void Close()`, `void Toggle()` | Show or hide. |
| `void CenterOnCar()` | Recenter and follow the car. |
| `void SetZoomMeters(float meters)` | Set zoom. |
| `void Pan(Vector2 screenDelta)` | Pan by a screen delta. |
| `void Zoom(float factor, Vector2 screenPivot)` | Zoom around a screen point. |
| `void Tap(Vector2 pos)`, `void TapAt(Vector2 screenPoint)` | Tap a marker or a position to preview a route. |
| `void SetCrosshairMode(bool active)` | Show or hide the crosshair. |
| `void ConfirmAtCrosshair()` | Preview or confirm at the crosshair. |
| `void PanByStick(Vector2 stick, float deltaTime)` | Gamepad pan. |
| `void ZoomBySpeed(float axis, float deltaTime)` | Gamepad zoom. |
| `float ZoomMeters` | Current zoom. |
| `bool IsFollowingCar`, `bool IsCrosshairActive` | Current state. |
| `FullMapZoomOutMode ZoomOutMode`, `CrosshairMode CrosshairMode` | Configuration. |
| `float OpenZoomMeters`, `MouseWheelStep`, `DoubleTapStep`, `MarkerTapRadius`, `bool ConfirmStep` | Configuration. |

## MinimapTapToOpen

`MonoBehaviour`. Opens the full map when the minimap is tapped.

| Member | Description |
| --- | --- |
| `MinimapTapAction TapAction` | Open full map or nothing. |
| `void SetTapAction(MinimapTapAction value)` | Set the action. |
| `void SetFullMap(MapViewInteractive value)` | Set which full map opens. |

## MinimapShape

`MonoBehaviour`. Masks the minimap to a rectangle or a sprite shape.

| Member | Description |
| --- | --- |
| `MinimapShapeKind ShapeKind` | Current kind. |
| `void SetShapeKind(MinimapShapeKind value)` | Rectangle or sprite mask. |
| `void SetSprite(Sprite value)` | Sprite used by the sprite mask. |

## Formatters and text

| Type | Description |
| --- | --- |
| `NavigationFormatter` | Abstract `ScriptableObject`. Override `FormatDistance(float meters, StringBuilder output)` and `FormatDuration(float seconds, StringBuilder output)` to change how values are written, then pass it to `NavigationManager.SetFormatter`. |
| `DefaultNavigationFormatter` | Metric or imperial output following `NavigationSettings.ImperialUnits`. |
| `NavigationTextTarget` | Abstract `MonoBehaviour`. Override `SetText(StringBuilder text)` to display navigation text in your own UI. |
| `LegacyTextTarget` | Writes to a `UnityEngine.UI.Text`. |
| `NavigationControls`, `CompassButton`, `PreviewPanel`, `SafeAreaFitter`, `MarkerLayer` | Prefab components. Configure in the Inspector, no scripting needed. |

## RouteStyle

`ScriptableObject`. Look of the route line.

| Property | Description |
| --- | --- |
| `Color ActiveLineColor`, `ActiveOutlineColor` | Active route colors. |
| `Color PreviewLineColor`, `PreviewOutlineColor` | Preview route colors. |
| `Color FadedColor` | Color of the driven part when `DrivenMode` is `Faded`. |
| `RouteDrivenMode DrivenMode` | Remove or fade the driven part. |
| `float HalfWidth`, `OutlineWidth`, `DashLength`, `GapLength` | Line geometry. |
| `Shader LineShader` | Shader used. |

## GamepadInputAdapter

Namespace `Gley.NavigationSystem.InputSystem`. Requires the Input System package. Added to the full map by the setup window. Drives `MapViewInteractive` from `NavigationMapControls.inputactions` (pan, zoom, crosshair, confirm).

## WorldConverter

Converts between Unity world positions and true meters, including origin shift.

| Member | Description |
| --- | --- |
| `Vector3 WorldToTrue(Vector3 world)`, `Vector3 TrueToWorld(Vector3 truePos)` | Position conversion. |
| `Vector3 WorldDirectionToTrue(Vector3 dir)` | Direction conversion. |
| `float WorldDistanceToTrue(float distance)` | Distance conversion. |
| `Vector3 Shift`, `float UnitsPerMeter` | Current values. |
| `void SetShift(Vector3 value)`, `void SetUnitsPerMeter(float value)` | Setters. |

## MapFrame

Converts between true positions and map positions for one map.

| Member | Description |
| --- | --- |
| `Vector3 Center`, `Vector2 Size`, `float RotationY` | Frame definition. |
| `Vector2 TrueToMap(Vector3 truePos)`, `Vector3 MapToTrue(Vector2 mapPos, float y)` | Position conversion. |
| `Vector2 TrueDirectionToMap(Vector3 dir)`, `Vector3 MapDirectionToTrue(Vector2 dir)` | Direction conversion. |
| `Vector2 MapToNormalized(Vector2 mapPos)` | 0..1 across the map. |
| `bool ContainsMap(Vector2 mapPos)` | Inside the map rectangle. |
| `float HeadingToMapAngle(Vector3 trueDir)` | Heading as a map angle. |

## SpeedUnits

| Method | Description |
| --- | --- |
| `float KmhToMetersPerSecond(float kmh)` | km/h to m/s. |
| `float MphToMetersPerSecond(float mph)` | mph to m/s. |
| `float MetersPerSecondToKmh(float metersPerSecond)` | m/s to km/h. |
| `float MetersPerSecondToMph(float metersPerSecond)` | m/s to mph. |

## Advanced types

These are public for testing and custom tooling. Normal integrations never need them, and they may change between versions.

| Area | Types |
| --- | --- |
| Pathfinding | `Pathfinder`, `RouteRequest`, `RouteSnapper` |
| Tracking | `RoadMatcher`, `VehicleMotion`, `NavigationSession`, `RerouteDecider` |
| Road data | `RoadNetworkData`, `RoadNetworkBuilder`, `RoadNetworkBuildInput`, `RoadRecord`, `IntersectionRecord`, `RoadPoint`, `RoadGrid`, `RoadQuery` |
| Markers | `MarkerRegistry`, `MarkerEntry`, `MarkerGrid` |
| Origin shift | `OriginShiftTracker`, `ShiftSource` |
| UI internals | `RouteLineRenderer`, `RouteLineGraphic`, `RouteLineMeshBuilder`, `RouteLineChunker`, `PointerInputAdapter`, `GestureTracker`, `MapViewMath`, `MinimapMath`, `FullMapMath`, `OffScreenArrowMath`, `CrosshairModeLogic` |
| Core | `CommandQueue`, `NavigationCommand`, `IFormatVersioned` |
