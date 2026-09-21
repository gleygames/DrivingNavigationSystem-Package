using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    public class NavigationSettings : ScriptableObject, IFormatVersioned
    {
        public const int CurrentFormatVersion = 1;
        public const int ChannelCount = 8;

        [SerializeField] private List<RoadType> roadTypes = new List<RoadType>();
        [SerializeField] private string[] viewChannelNames = new string[ChannelCount];
        [SerializeField] private float unitsPerMeter = 1f;
        [SerializeField] private int formatVersion = CurrentFormatVersion;
        [SerializeField] private int nextRoadTypeId;
        [SerializeField] private int version;

        public IReadOnlyList<RoadType> RoadTypes { get { return roadTypes; } }
        public float UnitsPerMeter { get { return unitsPerMeter; } }
        public int FormatVersion { get { return formatVersion; } }
        int IFormatVersioned.CurrentFormatVersion { get { return CurrentFormatVersion; } }
        public int NextRoadTypeId { get { return nextRoadTypeId; } }
        public int Version { get { return version; } }

        public void ResetToDefaults()
        {
            SpeedUnits speedUnits = new SpeedUnits();

            roadTypes.Clear();
            roadTypes.Add(new RoadType(1, "Highway", speedUnits.KmhToMetersPerSecond(110f), 20f, Color.red));
            roadTypes.Add(new RoadType(2, "Main", speedUnits.KmhToMetersPerSecond(60f), 14f, new Color(1f, 0.5f, 0f)));
            roadTypes.Add(new RoadType(3, "Secondary", speedUnits.KmhToMetersPerSecond(50f), 10f, Color.yellow));
            roadTypes.Add(new RoadType(4, "Local", speedUnits.KmhToMetersPerSecond(30f), 7f, new Color(0.8f, 0.8f, 0.8f)));
            nextRoadTypeId = 5;

            viewChannelNames[0] = "Minimap";
            viewChannelNames[1] = "Full map";
            for (int i = 2; i < ChannelCount; i++)
            {
                viewChannelNames[i] = "Custom " + (i - 1);
            }

            unitsPerMeter = 1f;
            formatVersion = CurrentFormatVersion;
            version = 0;
        }

        public RoadType AddRoadType(string name)
        {
            SpeedUnits speedUnits = new SpeedUnits();
            RoadType roadType = new RoadType(nextRoadTypeId, name, speedUnits.KmhToMetersPerSecond(50f), 10f, Color.white);
            roadTypes.Add(roadType);
            nextRoadTypeId++;
            version++;
            return roadType;
        }

        public RoadType FindRoadType(int id)
        {
            int index = FindIndexById(id);
            if (index < 0)
            {
                return null;
            }
            return roadTypes[index];
        }

        private int FindIndexById(int id)
        {
            for (int i = 0; i < roadTypes.Count; i++)
            {
                if (roadTypes[i].Id == id)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetRoadTypeIndex(int id)
        {
            return FindIndexById(id);
        }

        public void MoveRoadType(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= roadTypes.Count)
            {
                return;
            }
            if (toIndex < 0 || toIndex >= roadTypes.Count)
            {
                return;
            }

            RoadType roadType = roadTypes[fromIndex];
            roadTypes.RemoveAt(fromIndex);
            roadTypes.Insert(toIndex, roadType);
        }

        public bool RemoveRoadType(int id)
        {
            if (roadTypes.Count <= 1)
            {
                return false;
            }

            int index = FindIndexById(id);
            if (index < 0)
            {
                return false;
            }

            roadTypes.RemoveAt(index);
            version++;
            return true;
        }

        public void SetRoadTypeSpeed(int id, float value)
        {
            RoadType roadType = FindRoadType(id);
            if (roadType == null)
            {
                return;
            }

            roadType.SetSpeed(value);
            version++;
        }

        public void SetRoadTypeWidth(int id, float value)
        {
            RoadType roadType = FindRoadType(id);
            if (roadType == null)
            {
                return;
            }

            roadType.SetWidth(value);
            version++;
        }

        public string GetChannelName(int index)
        {
            return viewChannelNames[index];
        }
    }
}
