using Gley.Common.Editor;

namespace Gley.NavigationSystem.Editor
{
    public class NavigationWindowProperties : ISettingsWindowProperties
    {
        public const string MenuItem = "Tools/Gley/Navigation System/Road Editor";

        public string VersionFilePath { get { return "/Scripts/Version.txt"; } }
        public string WindowName { get { return "Navigation System - v."; } }
        public int MinWidth { get { return 420; } }
        public int MinHeight { get { return 480; } }
        public string FolderName { get { return "DrivingNavigationSystem"; } }
        public string ParentFolder { get { return "Gley"; } }
    }
}
