using Gley.Common.Editor;

namespace Gley.NavigationSystem.Editor
{
    public class NavigationVersion : IVersion
    {
        public string FolderName { get { return "DrivingNavigationSystem"; } }
        public string LongVersion { get { return "0.1.1"; } }
        public int ShortVersion { get { return 1; } }
    }
}
