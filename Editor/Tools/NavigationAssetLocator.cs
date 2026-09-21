using UnityEditor;
using UnityEngine;
using Gley.Common;

namespace Gley.NavigationSystem.Editor
{
    public class NavigationAssetLocator
    {
        public const string DefaultRootFolder = "Assets/NavigationData";
        private const string SettingsAssetName = "NavigationSettings";

        private readonly string rootFolder;

        public NavigationAssetLocator() : this(DefaultRootFolder)
        {
        }

        public NavigationAssetLocator(string rootFolder)
        {
            this.rootFolder = rootFolder;
        }

        public string GetDefaultMapFolder(string sceneName)
        {
            return rootFolder + "/" + sceneName;
        }

        public NavigationMapAssets CreateMapAssets(string folder, string name)
        {
            EnsureFolderExists(folder);

            MapData mapAsset = ScriptableObject.CreateInstance<MapData>();
            AssetDatabase.CreateAsset(mapAsset, folder + "/" + name + "_Map.asset");

            RoadNetworkData runtimeAsset = ScriptableObject.CreateInstance<RoadNetworkData>();
            AssetDatabase.CreateAsset(runtimeAsset, folder + "/" + name + "_RoadsRuntime.asset");

            RoadNetworkAuthoring authoringAsset = ScriptableObject.CreateInstance<RoadNetworkAuthoring>();
            AssetDatabase.CreateAsset(authoringAsset, folder + "/" + name + "_RoadsAuthoring.asset");

            authoringAsset.SetRuntimeAsset(runtimeAsset);
            authoringAsset.SetMapAsset(mapAsset);
            mapAsset.SetRoadNetwork(runtimeAsset);

            EditorUtility.SetDirty(mapAsset);
            EditorUtility.SetDirty(authoringAsset);
            AssetDatabase.SaveAssets();

            return new NavigationMapAssets(mapAsset, authoringAsset, runtimeAsset);
        }

        public NavigationSettings FindOrCreateSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:NavigationSettings");
            if (guids.Length == 0)
            {
                EnsureFolderExists(rootFolder);

                NavigationSettings created = ScriptableObject.CreateInstance<NavigationSettings>();
                created.ResetToDefaults();
                AssetDatabase.CreateAsset(created, rootFolder + "/" + SettingsAssetName + ".asset");
                AssetDatabase.SaveAssets();
                return created;
            }

            if (guids.Length > 1)
            {
                CustomLogger.LogWarning("NavigationAssetLocator: several NavigationSettings assets found; using the first by path.");
            }

            string[] paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }
            System.Array.Sort(paths);

            return AssetDatabase.LoadAssetAtPath<NavigationSettings>(paths[0]);
        }

        public RoadNetworkAuthoring FindAuthoringFor(RoadNetworkData runtime)
        {
            string[] guids = AssetDatabase.FindAssets("t:RoadNetworkAuthoring");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RoadNetworkAuthoring authoring = AssetDatabase.LoadAssetAtPath<RoadNetworkAuthoring>(path);
                if (authoring != null && authoring.RuntimeAsset == runtime)
                {
                    return authoring;
                }
            }
            return null;
        }

        private void EnsureFolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] segments = folder.Split('/');
            string currentPath = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string nextPath = currentPath + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[i]);
                }
                currentPath = nextPath;
            }
        }
    }
}
