using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    [FilePath(DrivingNavigationSystemDataPaths.UserSettingsFile, FilePathAttribute.Location.ProjectFolder)]
    public class NavigationUserSettings : ScriptableSingleton<NavigationUserSettings>
    {
        [SerializeField] private int roadLayers = -1;
        [SerializeField] private string captureSettingsJson = string.Empty;
        [SerializeField] private int brushTypeId;

        public int RoadLayers
        {
            get { return roadLayers; }
            set
            {
                if (value == roadLayers)
                {
                    return;
                }

                roadLayers = value;
                Save(true);
            }
        }

        public string CaptureSettingsJson
        {
            get { return captureSettingsJson; }
            set
            {
                string newJson = string.Empty;
                if (value != null)
                {
                    newJson = value;
                }

                if (newJson == captureSettingsJson)
                {
                    return;
                }

                captureSettingsJson = newJson;
                Save(true);
            }
        }

        public int BrushTypeId
        {
            get { return brushTypeId; }
            set
            {
                if (value == brushTypeId)
                {
                    return;
                }

                brushTypeId = value;
                Save(true);
            }
        }
    }
}
