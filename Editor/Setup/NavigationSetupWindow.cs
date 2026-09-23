using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Gley.Common;
using Gley.Common.Editor;

namespace Gley.NavigationSystem.Editor
{
    public class NavigationSetupWindow : EditorWindow
    {
        private const string FallbackRootFolder = "Assets/Gley/DrivingNavigationSystem";
        private static readonly string RootFolder = ResolveRootFolder();
        private static readonly string PrefabFolder = RootFolder + "/Graphics/Prefabs";
        private static readonly string MinimapPrefabPath = PrefabFolder + "/NavigationMinimap.prefab";
        private static readonly string FullMapPrefabPath = PrefabFolder + "/NavigationFullMap.prefab";
        private static readonly string PlayerMarkerPrefabPath = PrefabFolder + "/PlayerMarker.prefab";
        private static readonly string DestinationMarkerPrefabPath = PrefabFolder + "/DestinationMarker.prefab";
        private static readonly string PreviewPinPrefabPath = PrefabFolder + "/PreviewPin.prefab";
        private static readonly string DefaultFormatterPath = RootFolder + "/Graphics/Presets/DefaultFormatter.asset";
        private static readonly string InputActionsPath = RootFolder + "/Runtime.InputSystem/NavigationMapControls.inputactions";
        private const string GamepadAdapterTypeName = "Gley.NavigationSystem.InputSystem.GamepadInputAdapter, Gley.NavigationSystem.InputSystem";
        private const string InputSystemUIModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";
        private const string InputSystemAssemblyName = "Unity.InputSystem";
        private const string TmpTypeName = "TMPro.TextMeshProUGUI, Unity.TextMeshPro";
        private const string TrafficSystemDefine = "GLEY_TRAFFIC_SYSTEM";
        private const float DefaultRectangleSizeMeters = 200f;
        private const float DefaultMinimapWidthCanvas = 300f;
        private const float DefaultFullMapWidthCanvas = 1920f;
        private const float DefaultMinZoomMeters = 50f;
        private const float GizmoArrowLength = 3f;

        private readonly float[] quickYawAngles = new float[] { 0f, 90f, 180f, 270f };

        private List<ValidationIssue> validationIssues;
        private List<string> assemblyNames;
        private NavigationAssetLocator locator;
        private SetupStatusEvaluator evaluator;
        private BakeStatus bakeStatus;
        private RoadValidator validator;
        private RoadBaker baker;
        private MapRectangleSync rectangleSync;
        private MapImagePanel mapImagePanel;
        private MapImageImportSettings importSettingsChecker;
        private NavigationEditorPrefs editorPrefs;
        private FormatMigrator formatMigrator;
        private NavigationSettings settings;
        private NavigationMap targetMap;
        private RoadNetworkAuthoring authoringAsset;
        private NavigationManager managerInScene;
        private Canvas targetCanvas;
        private GameObject previewCarPrefab;
        private Bounds previewCarBounds;
        private Vector2 scrollPosition;
        private string mapFolder;
        private string mapName;
        private float pendingYawOffset;
        private bool carSpawnedAtRuntime;

        [MenuItem(NavigationSetupWindowProperties.MenuItem, false, 1)]
        private static void OpenWindow()
        {
            NavigationSetupWindowProperties properties = new NavigationSetupWindowProperties();
            NavigationSetupWindow window = GetWindow<NavigationSetupWindow>();
            window.titleContent = new GUIContent(properties.WindowName + new NavigationVersion().LongVersion);
            window.minSize = new Vector2(properties.MinWidth, properties.MinHeight);
            window.Show();
        }

        private void OnEnable()
        {
            locator = new NavigationAssetLocator();
            evaluator = new SetupStatusEvaluator();
            bakeStatus = new BakeStatus();
            validator = new RoadValidator();
            baker = new RoadBaker();
            rectangleSync = new MapRectangleSync();
            editorPrefs = new NavigationEditorPrefs();
            mapImagePanel = new MapImagePanel(editorPrefs);
            importSettingsChecker = new MapImageImportSettings();
            formatMigrator = new FormatMigrator(new List<IFormatMigration>());
            validationIssues = new List<ValidationIssue>();
            assemblyNames = new List<string>();
            mapFolder = locator.GetDefaultMapFolder(EditorSceneManager.GetActiveScene().name);
            mapName = "Map";

            RefreshState();
            RunOneTimeChecks();

            SceneView.duringSceneGui += HandleSceneGUI;
        }

        private void RefreshState()
        {
            targetMap = FindAnyObjectByType<NavigationMap>();
            settings = locator.FindOrCreateSettings();
            managerInScene = FindAnyObjectByType<NavigationManager>();
            if (targetCanvas == null)
            {
                targetCanvas = FindAnyObjectByType<Canvas>();
            }

            authoringAsset = null;
            if (targetMap != null && targetMap.MapData != null && targetMap.MapData.RoadNetwork != null)
            {
                authoringAsset = locator.FindAuthoringFor(targetMap.MapData.RoadNetwork);
            }
            AssignSettingsIfMissing();

            validationIssues.Clear();
            if (authoringAsset != null)
            {
                validator.RunFullChecks(authoringAsset, targetMap.MapData, validationIssues);
            }

            assemblyNames.Clear();
            UnityEditor.Compilation.Assembly[] assemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                assemblyNames.Add(assemblies[i].name);
            }

            if (managerInScene != null)
            {
                SerializedObject serializedManager = new SerializedObject(managerInScene);
                pendingYawOffset = serializedManager.FindProperty("carYawOffset").floatValue;
                carSpawnedAtRuntime = serializedManager.FindProperty("carSpawnedAtRuntime").boolValue;
            }

            EnsureMarkerPrefabs();
        }

        private void EnsureMarkerPrefabs()
        {
            if (managerInScene == null)
            {
                return;
            }

            SerializedObject serializedManager = new SerializedObject(managerInScene);
            bool changed = false;
            changed |= AssignPrefabIfMissing(serializedManager, "playerMarkerPrefab", PlayerMarkerPrefabPath);
            changed |= AssignPrefabIfMissing(serializedManager, "destinationMarkerPrefab", DestinationMarkerPrefabPath);
            changed |= AssignPrefabIfMissing(serializedManager, "previewPinPrefab", PreviewPinPrefabPath);
            if (changed)
            {
                serializedManager.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(managerInScene);
            }
        }

        private bool AssignPrefabIfMissing(SerializedObject serializedManager, string fieldName, string prefabPath)
        {
            SerializedProperty property = serializedManager.FindProperty(fieldName);
            if (property.objectReferenceValue != null)
            {
                return false;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return false;
            }

            property.objectReferenceValue = prefab;
            return true;
        }

        private void AssignSettingsIfMissing()
        {
            if (authoringAsset == null || authoringAsset.Settings != null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(authoringAsset);
            serializedObject.FindProperty("settings").objectReferenceValue = settings;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(authoringAsset);
        }

        private void RunOneTimeChecks()
        {
            formatMigrator.MigrateIfNeeded(settings);
            if (targetMap != null && targetMap.MapData != null)
            {
                formatMigrator.MigrateIfNeeded(targetMap.MapData);
            }
            if (authoringAsset != null)
            {
                formatMigrator.MigrateIfNeeded(authoringAsset);
            }

            string[] routeStyleGuids = AssetDatabase.FindAssets("t:RouteStyle");
            for (int i = 0; i < routeStyleGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(routeStyleGuids[i]);
                UnityEngine.Object routeStyleAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                formatMigrator.MigrateIfNeeded(routeStyleAsset);
            }

            bool trafficSystemPresent = evaluator.IsTrafficSystemPresent(assemblyNames);
            PreprocessorDirective.AddToCurrent(TrafficSystemDefine, !trafficSystemPresent);
        }

        private void HandleSceneGUI(SceneView sceneView)
        {
            if (managerInScene == null)
            {
                return;
            }

            Transform car = managerInScene.Car;
            if (car != null)
            {
                Vector3 direction = car.rotation * Quaternion.Euler(0f, pendingYawOffset, 0f) * Vector3.forward;
                Handles.color = Color.cyan;
                Handles.ArrowHandleCap(0, car.position, Quaternion.LookRotation(direction), GizmoArrowLength, EventType.Repaint);
                return;
            }

            if (carSpawnedAtRuntime && previewCarPrefab != null)
            {
                Vector3 previewDirection = Quaternion.Euler(0f, pendingYawOffset, 0f) * Vector3.forward;
                Handles.color = Color.cyan;
                Handles.DrawWireCube(previewCarBounds.center, previewCarBounds.size);
                Handles.ArrowHandleCap(0, previewCarBounds.center, Quaternion.LookRotation(previewDirection), GizmoArrowLength, EventType.Repaint);
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawStep1MapArea();
            EditorGUILayout.Space();
            DrawStep2MapImage();
            EditorGUILayout.Space();
            DrawStep3Roads();
            EditorGUILayout.Space();
            DrawStep4Ui();
            EditorGUILayout.Space();
            DrawStep5Car();

            EditorGUILayout.EndScrollView();
        }

        private void DrawStepHeader(int number, string title, SetupStatus status)
        {
            string label = "Step " + number + ": " + title + " - " + status;
            if (status == SetupStatus.Warning)
            {
                EditorGUILayout.HelpBox(label, MessageType.Warning);
            }
            else if (status == SetupStatus.Missing)
            {
                EditorGUILayout.HelpBox(label, MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            }
        }

        private void DrawStep1MapArea()
        {
            bool mapObjectExists = targetMap != null;
            bool mapAssetExists = targetMap != null && targetMap.MapData != null;
            SetupStatus status = evaluator.EvaluateMapArea(mapObjectExists, mapAssetExists);
            DrawStepHeader(1, "Map area", status);

            if (status == SetupStatus.Done)
            {
                EditorGUILayout.LabelField("Map", targetMap.MapData.name);
                return;
            }

            mapFolder = EditorGUILayout.TextField("Folder", mapFolder);
            mapName = EditorGUILayout.TextField("Name", mapName);

            if (GUILayout.Button("Create map"))
            {
                CreateMapAndObjects();
            }
        }

        private void CreateMapAndObjects()
        {
            string folder = mapFolder;
            if (string.IsNullOrEmpty(folder))
            {
                folder = locator.GetDefaultMapFolder(EditorSceneManager.GetActiveScene().name);
            }
            string name = mapName;
            if (string.IsNullOrEmpty(name))
            {
                name = "Map";
            }

            NavigationMapAssets assets = locator.CreateMapAssets(folder, name);
            ApplyInitialRectangle(assets.MapAsset);

            GameObject mapObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(mapObject, "Create Navigation Map");
            NavigationMap mapComponent = mapObject.AddComponent<NavigationMap>();
            mapComponent.SetMapData(assets.MapAsset);
            rectangleSync.SnapObjectToAsset(mapObject.transform, assets.MapAsset, settings.UnitsPerMeter);
            EditorUtility.SetDirty(assets.MapAsset);

            EnsureManager();

            RefreshState();
        }

        private void ApplyInitialRectangle(MapData mapAsset)
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (renderers.Length == 0)
            {
                mapAsset.SetRectangleCenter(Vector3.zero);
                mapAsset.SetRectangleSize(new Vector2(DefaultRectangleSizeMeters, DefaultRectangleSizeMeters));
                return;
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            WorldConverter converter = new WorldConverter();
            converter.SetUnitsPerMeter(settings.UnitsPerMeter);
            Vector3 trueMin = converter.WorldToTrue(worldBounds.min);
            Vector3 trueMax = converter.WorldToTrue(worldBounds.max);

            Vector3 center = (trueMin + trueMax) * 0.5f;
            Vector2 size = new Vector2(trueMax.x - trueMin.x, trueMax.z - trueMin.z);
            if (size.x < DefaultRectangleSizeMeters)
            {
                size.x = DefaultRectangleSizeMeters;
            }
            if (size.y < DefaultRectangleSizeMeters)
            {
                size.y = DefaultRectangleSizeMeters;
            }

            mapAsset.SetRectangleCenter(center);
            mapAsset.SetRectangleSize(size);
        }

        private void EnsureManager()
        {
            if (managerInScene != null)
            {
                return;
            }

            GameObject managerObject = new GameObject("NavigationManager");
            Undo.RegisterCreatedObjectUndo(managerObject, "Create Navigation Manager");
            NavigationManager manager = managerObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);

            DefaultNavigationFormatter formatter = AssetDatabase.LoadAssetAtPath<DefaultNavigationFormatter>(DefaultFormatterPath);
            if (formatter != null)
            {
                formatter.SetSettings(settings);
                manager.SetFormatter(formatter);
            }

            managerInScene = manager;
        }

        private void DrawStep2MapImage()
        {
            if (targetMap == null || targetMap.MapData == null)
            {
                DrawStepHeader(2, "Map image", SetupStatus.Missing);
                EditorGUILayout.HelpBox("Create the map area first.", MessageType.Info);
                return;
            }

            MapData data = targetMap.MapData;
            bool hasImportWarnings = HasImageImportWarnings(data);
            SetupStatus status = evaluator.EvaluateMapImage(data.ImageState, hasImportWarnings);
            DrawStepHeader(2, "Map image", status);

            mapImagePanel.Draw(data, settings.UnitsPerMeter);
            DrawBlurInfo(data);
        }

        private bool HasImageImportWarnings(MapData data)
        {
            if (data.Image == null)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(data.Image);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            List<string> warnings = new List<string>();
            importSettingsChecker.GetWarnings(assetPath, warnings);
            return warnings.Count > 0;
        }

        private void DrawBlurInfo(MapData data)
        {
            if (data.Image == null)
            {
                return;
            }

            float metersPerPixel = data.RectangleSize.x / data.Image.width;

            MapView[] views = FindObjectsByType<MapView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (views.Length == 0)
            {
                DrawBlurLine("Minimap (default)", DefaultMinimapWidthCanvas, DefaultMinZoomMeters, metersPerPixel);
                DrawBlurLine("Full map (default)", DefaultFullMapWidthCanvas, DefaultMinZoomMeters, metersPerPixel);
                return;
            }

            for (int i = 0; i < views.Length; i++)
            {
                MapView view = views[i];
                if (view.Viewport == null)
                {
                    continue;
                }

                float viewportWidth = view.Viewport.rect.width;
                float minZoomMeters = ReadMinZoomMeters(view);
                DrawBlurLine(view.name, viewportWidth, minZoomMeters, metersPerPixel);
            }
        }

        private float ReadMinZoomMeters(MapView view)
        {
            SerializedObject serializedView = new SerializedObject(view);
            return serializedView.FindProperty("minZoomMeters").floatValue;
        }

        private void DrawBlurLine(string label, float viewportWidthCanvas, float minZoomMeters, float metersPerPixel)
        {
            float factor = evaluator.BlurFactor(viewportWidthCanvas, minZoomMeters, metersPerPixel);
            string text = label + ": at max zoom-in, 1 image pixel = " + factor.ToString("0.##") + " screen pixels";
            if (evaluator.IsBlurry(factor))
            {
                EditorGUILayout.HelpBox(text + " (blurry)", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField(text);
            }
        }

        private void DrawStep3Roads()
        {
            if (authoringAsset == null)
            {
                DrawStepHeader(3, "Roads", SetupStatus.Missing);
                EditorGUILayout.HelpBox("Create the map area first.", MessageType.Info);
                return;
            }

            bool bakeOutdated = bakeStatus.IsOutdated(authoringAsset);
            SetupStatus status = evaluator.EvaluateRoads(authoringAsset.Roads.Count, bakeOutdated, validationIssues.Count);
            DrawStepHeader(3, "Roads", status);

            EditorGUILayout.LabelField("Road count", authoringAsset.Roads.Count.ToString());
            string bakeText;
            if (bakeOutdated)
            {
                bakeText = "Outdated";
            }
            else
            {
                bakeText = "Up to date";
            }
            EditorGUILayout.LabelField("Bake", bakeText);
            EditorGUILayout.LabelField("Validation issues", validationIssues.Count.ToString());

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Road Editor"))
            {
                NavigationWindowProperties properties = new NavigationWindowProperties();
                NavigationWindow window = EditorWindow.GetWindow<NavigationWindow>();
                window.titleContent = new GUIContent(properties.WindowName + new NavigationVersion().LongVersion);
                window.minSize = new Vector2(properties.MinWidth, properties.MinHeight);
                window.Show();
            }
            if (GUILayout.Button("Validate"))
            {
                validationIssues.Clear();
                validator.RunFullChecks(authoringAsset, targetMap.MapData, validationIssues);
            }
            if (GUILayout.Button("Bake"))
            {
                baker.Bake(authoringAsset);
                RefreshState();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStep4Ui()
        {
            bool minimapPresent = FindAnyObjectByType<MapViewFollowCar>(FindObjectsInactive.Include) != null;
            bool fullMapPresent = FindAnyObjectByType<MapViewInteractive>(FindObjectsInactive.Include) != null;
            InputModuleChoice desiredModule = ComputeDesiredInputModule();
            EventSystem existingEventSystem = FindAnyObjectByType<EventSystem>();
            bool eventSystemMismatch = false;
            if (existingEventSystem != null)
            {
                eventSystemMismatch = IsEventSystemModuleMismatched(existingEventSystem, desiredModule);
            }
            bool tmpMissing = IsTmpMissing();

            SetupStatus status = evaluator.EvaluateUi(minimapPresent, fullMapPresent, eventSystemMismatch, tmpMissing);
            DrawStepHeader(4, "UI", status);

            if (tmpMissing)
            {
                EditorGUILayout.HelpBox("TextMeshPro is not installed.", MessageType.Warning);
            }
            if (eventSystemMismatch)
            {
                EditorGUILayout.HelpBox("The existing Event System does not use the expected input module. It was not modified.", MessageType.Warning);
            }

            targetCanvas = (Canvas)EditorGUILayout.ObjectField("Canvas", targetCanvas, typeof(Canvas), true);
            if (GUILayout.Button("Create Canvas"))
            {
                targetCanvas = CreateCanvas();
            }

            if (GUILayout.Button("Add minimap and full map"))
            {
                AddMinimapAndFullMap(desiredModule);
                RefreshState();
            }
        }

        private InputModuleChoice ComputeDesiredInputModule()
        {
            bool newInputSystemEnabled = false;
#if ENABLE_INPUT_SYSTEM
            newInputSystemEnabled = true;
#endif
            bool inputSystemPackagePresent = IsAssemblyPresent(InputSystemAssemblyName);
            return evaluator.ChooseInputModule(newInputSystemEnabled, inputSystemPackagePresent);
        }

        private bool IsAssemblyPresent(string assemblyName)
        {
            for (int i = 0; i < assemblyNames.Count; i++)
            {
                if (assemblyNames[i] == assemblyName)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsEventSystemModuleMismatched(EventSystem eventSystem, InputModuleChoice desired)
        {
            StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            bool hasStandalone = standaloneModule != null;

            System.Type inputSystemModuleType = System.Type.GetType(InputSystemUIModuleTypeName);
            bool hasInputSystemModule = false;
            if (inputSystemModuleType != null)
            {
                hasInputSystemModule = eventSystem.GetComponent(inputSystemModuleType) != null;
            }

            if (desired == InputModuleChoice.Standalone)
            {
                return !hasStandalone;
            }
            return !hasInputSystemModule;
        }

        private bool IsTmpMissing()
        {
            return System.Type.GetType(TmpTypeName) == null;
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static string ResolveRootFolder()
        {
            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(NavigationSetupWindow).Assembly);
            if (package != null)
            {
                return package.assetPath;
            }
            return FallbackRootFolder;
        }

        private void AddMinimapAndFullMap(InputModuleChoice desired)
        {
            if (targetCanvas == null)
            {
                targetCanvas = CreateCanvas();
            }
            EnsureEventSystem(desired);

            GameObject minimapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinimapPrefabPath);
            GameObject fullMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FullMapPrefabPath);
            if (minimapPrefab == null || fullMapPrefab == null)
            {
                CustomLogger.LogError("NavigationSetupWindow: default prefabs are missing from " + PrefabFolder + ". Reimport the Driving Navigation System package.");
                return;
            }

            GameObject minimapInstance = (GameObject)PrefabUtility.InstantiatePrefab(minimapPrefab, targetCanvas.transform);
            Undo.RegisterCreatedObjectUndo(minimapInstance, "Add Minimap");
            GameObject fullMapInstance = (GameObject)PrefabUtility.InstantiatePrefab(fullMapPrefab, targetCanvas.transform);
            Undo.RegisterCreatedObjectUndo(fullMapInstance, "Add Full Map");

            MinimapTapToOpen tapToOpen = minimapInstance.GetComponentInChildren<MinimapTapToOpen>(true);
            MapViewInteractive interactive = fullMapInstance.GetComponentInChildren<MapViewInteractive>(true);
            if (tapToOpen != null && interactive != null)
            {
                tapToOpen.SetFullMap(interactive);
            }

            if (interactive != null && IsAssemblyPresent(InputSystemAssemblyName))
            {
                AddGamepadAdapter(interactive.gameObject);
            }
        }

        private void EnsureEventSystem(InputModuleChoice desired)
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create Event System");

            if (desired == InputModuleChoice.InputSystemUI)
            {
                System.Type moduleType = System.Type.GetType(InputSystemUIModuleTypeName);
                if (moduleType != null)
                {
                    eventSystemObject.AddComponent(moduleType);
                    return;
                }
            }

            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void AddGamepadAdapter(GameObject fullMapObject)
        {
            System.Type adapterType = System.Type.GetType(GamepadAdapterTypeName);
            if (adapterType == null)
            {
                return;
            }

            Component adapter = fullMapObject.AddComponent(adapterType);
            Undo.RegisterCreatedObjectUndo(adapter, "Add Gamepad Adapter");

            UnityEngine.Object actionsAsset = AssetDatabase.LoadAssetAtPath(InputActionsPath, typeof(UnityEngine.Object));

            SerializedObject serializedAdapter = new SerializedObject(adapter);
            serializedAdapter.FindProperty("actions").objectReferenceValue = actionsAsset;
            serializedAdapter.FindProperty("target").objectReferenceValue = fullMapObject.GetComponent<MapViewInteractive>();
            serializedAdapter.FindProperty("manager").objectReferenceValue = managerInScene;
            serializedAdapter.ApplyModifiedPropertiesWithoutUndo();
        }

        private void DrawStep5Car()
        {
            bool carAssigned = managerInScene != null && managerInScene.Car != null;
            SetupStatus status = evaluator.EvaluateCar(carAssigned, carSpawnedAtRuntime);
            DrawStepHeader(5, "Car", status);

            if (managerInScene == null)
            {
                EditorGUILayout.HelpBox("Add the UI step first to create the Navigation Manager.", MessageType.Info);
                return;
            }

            bool newSpawnedAtRuntime = EditorGUILayout.Toggle("Car spawned at runtime", carSpawnedAtRuntime);
            if (newSpawnedAtRuntime != carSpawnedAtRuntime)
            {
                carSpawnedAtRuntime = newSpawnedAtRuntime;
                WriteCarSpawnedAtRuntime();
            }

            if (carSpawnedAtRuntime)
            {
                DrawRuntimeCar();
                return;
            }

            DrawSceneCar();
        }

        private void DrawSceneCar()
        {
            Transform currentCar = managerInScene.Car;
            Transform newCar = (Transform)EditorGUILayout.ObjectField("Car", currentCar, typeof(Transform), true);

            DrawYawButtons(newCar);

            if (newCar != currentCar)
            {
                ApplyCar(newCar, pendingYawOffset);
            }
        }

        private void DrawRuntimeCar()
        {
            EditorGUILayout.HelpBox("After the car is spawned, call NavigationManager.SetCar(car.transform, navigationManager.CarYawOffset).", MessageType.Info);

            GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField("Preview prefab", previewCarPrefab, typeof(GameObject), false);
            if (newPrefab != previewCarPrefab)
            {
                previewCarPrefab = newPrefab;
                previewCarBounds = ComputePreviewBounds(previewCarPrefab);
                SceneView.RepaintAll();
            }

            DrawYawButtons(managerInScene.Car);
        }

        private void DrawYawButtons(Transform car)
        {
            EditorGUILayout.LabelField("Yaw offset", pendingYawOffset.ToString("0"));
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < quickYawAngles.Length; i++)
            {
                if (GUILayout.Button(quickYawAngles[i].ToString("0")))
                {
                    pendingYawOffset = quickYawAngles[i];
                    ApplyCar(car, pendingYawOffset);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyCar(Transform car, float yawOffset)
        {
            managerInScene.SetCarReference(car, yawOffset);
            EditorUtility.SetDirty(managerInScene);
            SceneView.RepaintAll();
        }

        private void WriteCarSpawnedAtRuntime()
        {
            SerializedObject serializedManager = new SerializedObject(managerInScene);
            serializedManager.FindProperty("carSpawnedAtRuntime").boolValue = carSpawnedAtRuntime;
            serializedManager.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }

        private Bounds ComputePreviewBounds(GameObject prefab)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            if (prefab == null)
            {
                return bounds;
            }

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return bounds;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private void OnSelectionChange()
        {
            RefreshState();
            Repaint();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= HandleSceneGUI;
            mapImagePanel.Dispose();
        }
    }
}
