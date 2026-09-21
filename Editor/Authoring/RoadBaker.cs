using Gley.Common;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class RoadBaker
    {
        private readonly RoadNetworkBuilder builder;

        public RoadBaker()
        {
            builder = new RoadNetworkBuilder();
        }

        public void Bake(RoadNetworkAuthoring authoring)
        {
            RoadNetworkBuildInput input = BuildInput(authoring);

            RoadNetworkData runtime = authoring.RuntimeAsset;
            if (runtime == null)
            {
                runtime = CreateRuntimeAsset(authoring);
                authoring.SetRuntimeAsset(runtime);
            }

            builder.Build(input, authoring.Settings, runtime, authoring.GridCellSize);

            runtime.SetSourceVersion(authoring.Version);
            runtime.SetFormatVersion(RoadNetworkData.CurrentFormatVersion);

            EditorUtility.SetDirty(runtime);
            EditorUtility.SetDirty(authoring);
            AssetDatabase.SaveAssets();
        }

        private RoadNetworkBuildInput BuildInput(RoadNetworkAuthoring authoring)
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();

            for (int i = 0; i < authoring.Intersections.Count; i++)
            {
                AuthoringIntersection intersection = authoring.Intersections[i];
                BuildIntersection buildIntersection = new BuildIntersection();
                buildIntersection.Id = intersection.Id;
                buildIntersection.Position = intersection.Position;
                input.Intersections.Add(buildIntersection);
            }

            for (int i = 0; i < authoring.Roads.Count; i++)
            {
                AuthoringRoad road = authoring.Roads[i];
                if (road.Points.Count < 2)
                {
                    CustomLogger.LogWarning("RoadBaker: road " + road.Id + " has fewer than 2 points and was skipped.");
                    continue;
                }

                BuildRoad buildRoad = new BuildRoad();
                buildRoad.Id = road.Id;
                buildRoad.TypeId = road.TypeId;
                buildRoad.OneWay = road.OneWay;
                buildRoad.StartIntersectionId = road.StartIntersectionId;
                buildRoad.EndIntersectionId = road.EndIntersectionId;
                buildRoad.SpeedOverride = road.SpeedOverride;
                buildRoad.WidthOverride = road.WidthOverride;
                for (int p = 0; p < road.Points.Count; p++)
                {
                    buildRoad.Points.Add(road.Points[p]);
                }
                input.Roads.Add(buildRoad);
            }

            return input;
        }

        private RoadNetworkData CreateRuntimeAsset(RoadNetworkAuthoring authoring)
        {
            RoadNetworkData runtime = ScriptableObject.CreateInstance<RoadNetworkData>();
            string path = BuildRuntimeAssetPath(authoring);
            AssetDatabase.CreateAsset(runtime, path);
            return runtime;
        }

        private string BuildRuntimeAssetPath(RoadNetworkAuthoring authoring)
        {
            string authoringPath = AssetDatabase.GetAssetPath(authoring);
            int lastSlash = authoringPath.LastIndexOf('/');
            string directory = authoringPath.Substring(0, lastSlash);
            string fileName = authoringPath.Substring(lastSlash + 1);
            int lastDot = fileName.LastIndexOf('.');
            string nameWithoutExtension = fileName.Substring(0, lastDot);

            string suffix = "_RoadsAuthoring";
            string runtimeName;
            if (nameWithoutExtension.EndsWith(suffix))
            {
                runtimeName = nameWithoutExtension.Substring(0, nameWithoutExtension.Length - suffix.Length) + "_RoadsRuntime";
            }
            else
            {
                runtimeName = nameWithoutExtension + "_RoadsRuntime";
            }

            return directory + "/" + runtimeName + ".asset";
        }
    }
}
