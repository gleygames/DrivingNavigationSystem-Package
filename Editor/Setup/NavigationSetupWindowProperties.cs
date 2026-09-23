using Gley.Common.Editor;

namespace Gley.NavigationSystem.Editor
{
    public class NavigationSetupWindowProperties : ISettingsWindowProperties
    {
        public const string MenuItem = "Tools/Gley/Navigation System/Setup";

        public string VersionFilePath { get { return "/Scripts/Version.txt"; } }
        public string WindowName { get { return "Navigation System Setup - v."; } }
        public int MinWidth { get { return 420; } }
        public int MinHeight { get { return 480; } }
        public string FolderName { get { return "DrivingNavigationSystem"; } }
        public string ParentFolder { get { return "Gley"; } }
    }
}
