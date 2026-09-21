using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Gley.Common;

namespace Gley.NavigationSystem.Editor
{
    public class BuildCheckEvaluator
    {
        private readonly List<ValidationIssue> issueBuffer;
        private readonly BakeStatus bakeStatus;
        private readonly RoadValidator validator;

        public BuildCheckEvaluator()
        {
            issueBuffer = new List<ValidationIssue>();
            bakeStatus = new BakeStatus();
            validator = new RoadValidator();
        }

        public bool Evaluate(List<RoadNetworkAuthoring> assets, NavigationSettings settings, List<string> messages)
        {
            messages.Clear();
            bool hasProblems = false;

            for (int i = 0; i < assets.Count; i++)
            {
                RoadNetworkAuthoring asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                if (EvaluateAsset(asset, messages))
                {
                    hasProblems = true;
                }
            }

            bool blockOnProblems = settings != null && settings.BlockBuildOnProblems;
            return hasProblems && blockOnProblems;
        }

        private bool EvaluateAsset(RoadNetworkAuthoring asset, List<string> messages)
        {
            string mapName = ResolveMapName(asset);
            bool hasProblems = false;

            if (asset.Settings == null || bakeStatus.IsOutdated(asset))
            {
                messages.Add("Bake outdated for map " + mapName + ".");
                hasProblems = true;
            }

            issueBuffer.Clear();
            validator.RunFullChecks(asset, asset.MapAsset, issueBuffer);
            if (issueBuffer.Count > 0)
            {
                messages.Add(issueBuffer.Count + " validation issue(s) for map " + mapName + ".");
                hasProblems = true;
            }

            return hasProblems;
        }

        private string ResolveMapName(RoadNetworkAuthoring asset)
        {
            if (asset.MapAsset != null)
            {
                return asset.MapAsset.name;
            }
            return asset.name;
        }
    }

    public class NavigationBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            List<RoadNetworkAuthoring> assets = FindAllAuthoringAssets();
            NavigationSettings settings = FindSettings();

            List<string> messages = new List<string>();
            BuildCheckEvaluator evaluator = new BuildCheckEvaluator();
            bool shouldBlock = evaluator.Evaluate(assets, settings, messages);

            if (messages.Count == 0)
            {
                return;
            }

            string summary = "Navigation System build check:\n" + string.Join("\n", messages);
            CustomLogger.LogWarning(summary);

            if (shouldBlock)
            {
                throw new BuildFailedException(summary);
            }
        }

        private List<RoadNetworkAuthoring> FindAllAuthoringAssets()
        {
            List<RoadNetworkAuthoring> assets = new List<RoadNetworkAuthoring>();
            string[] guids = AssetDatabase.FindAssets("t:RoadNetworkAuthoring");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RoadNetworkAuthoring asset = AssetDatabase.LoadAssetAtPath<RoadNetworkAuthoring>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            return assets;
        }

        private NavigationSettings FindSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:NavigationSettings");
            if (guids.Length == 0)
            {
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<NavigationSettings>(path);
        }
    }
}
