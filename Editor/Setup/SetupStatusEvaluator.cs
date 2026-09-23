using System.Collections.Generic;

namespace Gley.NavigationSystem.Editor
{
    public class SetupStatusEvaluator
    {
        private const string TrafficSystemAssemblyPrefix = "Gley.TrafficSystem";
        private const float BlurryThreshold = 2f;

        public SetupStatus EvaluateMapArea(bool mapObjectExists, bool mapAssetExists)
        {
            if (mapObjectExists && mapAssetExists)
            {
                return SetupStatus.Done;
            }
            return SetupStatus.Missing;
        }

        public SetupStatus EvaluateMapImage(MapImageState imageState, bool hasImportWarnings)
        {
            if (imageState == MapImageState.None)
            {
                return SetupStatus.Missing;
            }
            if (imageState == MapImageState.Outdated)
            {
                return SetupStatus.Warning;
            }
            if (hasImportWarnings)
            {
                return SetupStatus.Warning;
            }
            return SetupStatus.Done;
        }

        public SetupStatus EvaluateRoads(int roadCount, bool bakeOutdated, int validationIssueCount)
        {
            if (roadCount == 0)
            {
                return SetupStatus.Missing;
            }
            if (bakeOutdated || validationIssueCount > 0)
            {
                return SetupStatus.Warning;
            }
            return SetupStatus.Done;
        }

        public SetupStatus EvaluateUi(bool minimapPresent, bool fullMapPresent, bool eventSystemModuleMismatch, bool tmpMissing)
        {
            if (!minimapPresent || !fullMapPresent)
            {
                return SetupStatus.Missing;
            }
            if (eventSystemModuleMismatch || tmpMissing)
            {
                return SetupStatus.Warning;
            }
            return SetupStatus.Done;
        }

        public SetupStatus EvaluateCar(bool carAssigned)
        {
            if (carAssigned)
            {
                return SetupStatus.Done;
            }
            return SetupStatus.Missing;
        }

        public InputModuleChoice ChooseInputModule(bool newInputSystemEnabled, bool inputSystemPackagePresent)
        {
            if (newInputSystemEnabled && inputSystemPackagePresent)
            {
                return InputModuleChoice.InputSystemUI;
            }
            return InputModuleChoice.Standalone;
        }

        public bool IsTrafficSystemPresent(List<string> assemblyNames)
        {
            for (int i = 0; i < assemblyNames.Count; i++)
            {
                if (assemblyNames[i].StartsWith(TrafficSystemAssemblyPrefix))
                {
                    return true;
                }
            }
            return false;
        }

        public float BlurFactor(float viewportWidthCanvas, float minZoomMeters, float metersPerPixel)
        {
            return (viewportWidthCanvas / minZoomMeters) * metersPerPixel;
        }

        public bool IsBlurry(float factor)
        {
            return factor > BlurryThreshold;
        }
    }
}
