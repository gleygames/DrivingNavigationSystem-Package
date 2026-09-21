using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gley.NavigationSystem.Editor
{
    [InitializeOnLoad]
    public class MapSceneHooks
    {
        static MapSceneHooks()
        {
            new MapSceneHooks();
        }

        public MapSceneHooks()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            MapRectangleSync sync = new MapRectangleSync();
            float unitsPerMeter = ResolveUnitsPerMeter();

            NavigationMap[] maps = Object.FindObjectsOfType<NavigationMap>();
            for (int i = 0; i < maps.Length; i++)
            {
                NavigationMap map = maps[i];
                if (map.MapData == null)
                {
                    continue;
                }
                sync.SnapObjectToAsset(map.transform, map.MapData, unitsPerMeter);
            }
        }

        private float ResolveUnitsPerMeter()
        {
            string[] guids = AssetDatabase.FindAssets("t:NavigationSettings");
            if (guids.Length == 0)
            {
                return 1f;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            NavigationSettings settings = AssetDatabase.LoadAssetAtPath<NavigationSettings>(path);
            if (settings == null)
            {
                return 1f;
            }
            return settings.UnitsPerMeter;
        }
    }
}
