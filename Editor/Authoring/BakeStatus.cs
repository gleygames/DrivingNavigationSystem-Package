namespace Gley.NavigationSystem.Editor
{
    public class BakeStatus
    {
        public bool IsOutdated(RoadNetworkAuthoring authoring)
        {
            RoadNetworkData runtime = authoring.RuntimeAsset;
            if (runtime == null)
            {
                return true;
            }

            if (runtime.SourceVersion != authoring.Version)
            {
                return true;
            }

            if (runtime.SettingsVersion != authoring.Settings.Version)
            {
                return true;
            }

            if (runtime.FormatVersion != RoadNetworkData.CurrentFormatVersion)
            {
                return true;
            }

            return false;
        }
    }
}
