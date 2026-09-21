using System.Collections.Generic;

namespace Gley.NavigationSystem.Editor
{
    public class RoadEditorContext
    {
        public RoadEditorContext(NavigationEditorPrefs prefs, List<ValidationIssue> validationIssues)
        {
            Prefs = prefs;
            ValidationIssues = validationIssues;
        }

        public List<ValidationIssue> ValidationIssues { get; }
        public NavigationEditorPrefs Prefs { get; }
        public RoadNetworkAuthoring Authoring { get; private set; }
        public MapData MapData { get; private set; }

        public float UnitsPerMeter
        {
            get
            {
                if (Authoring == null || Authoring.Settings == null)
                {
                    return 1f;
                }
                return Authoring.Settings.UnitsPerMeter;
            }
        }

        public void SetTarget(RoadNetworkAuthoring authoring, MapData mapData)
        {
            Authoring = authoring;
            MapData = mapData;
        }
    }
}
