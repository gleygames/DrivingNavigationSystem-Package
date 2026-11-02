namespace Gley.NavigationSystem.Editor
{
    public class DrivingNavigationSystemDataPaths
    {
        public const string UserSettingsFile = "UserSettings/Gley/DrivingNavigationSystem.asset";

        private const string DefaultRoot = "Assets/GleyData/DrivingNavigationSystem";
        private const string MapsFolderName = "Maps";
        private const string SettingsAssetName = "NavigationSettings";

        public string Root { get; }
        public string MapsFolder { get; }
        public string SettingsAssetPath { get; }

        public DrivingNavigationSystemDataPaths() : this(DefaultRoot)
        {
        }

        public DrivingNavigationSystemDataPaths(string root)
        {
            Root = root;
            MapsFolder = Root + "/" + MapsFolderName;
            SettingsAssetPath = Root + "/" + SettingsAssetName + ".asset";
        }

        public string MapFolder(string sceneName)
        {
            return MapsFolder + "/" + sceneName;
        }
    }
}
