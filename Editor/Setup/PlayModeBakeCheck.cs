using UnityEditor;
using UnityEngine;
using Gley.Common;

namespace Gley.NavigationSystem.Editor
{
    [InitializeOnLoad]
    public class PlayModeBakeCheck
    {
        static PlayModeBakeCheck()
        {
            new PlayModeBakeCheck();
        }

        public PlayModeBakeCheck()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            NavigationAssetLocator locator = new NavigationAssetLocator();
            BakeStatus bakeStatus = new BakeStatus();

            NavigationMap[] maps = Object.FindObjectsOfType<NavigationMap>();
            for (int i = 0; i < maps.Length; i++)
            {
                CheckMap(maps[i], locator, bakeStatus);
            }
        }

        private void CheckMap(NavigationMap map, NavigationAssetLocator locator, BakeStatus bakeStatus)
        {
            if (map.MapData == null || map.MapData.RoadNetwork == null)
            {
                return;
            }

            RoadNetworkAuthoring authoring = locator.FindAuthoringFor(map.MapData.RoadNetwork);
            if (authoring == null || authoring.Settings == null)
            {
                return;
            }

            if (bakeStatus.IsOutdated(authoring))
            {
                CustomLogger.LogWarning("Bake outdated for map " + map.MapData.name + ".");
            }
        }
    }
}
