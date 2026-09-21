using System.Collections.Generic;

namespace Gley.NavigationSystem
{
    public class RoutePreferences
    {
        private readonly List<int> typeIds;
        private readonly List<RoadTypePreference> preferences;

        public RouteMode Mode { get; set; }
        public UTurnRule UTurn { get; set; }
        public float AvoidMultiplier { get; set; }
        public float PreferMultiplier { get; set; }

        public RoutePreferences()
        {
            typeIds = new List<int>();
            preferences = new List<RoadTypePreference>();
            AvoidMultiplier = 5f;
            PreferMultiplier = 0.7f;
        }

        public void SetPreference(int typeId, RoadTypePreference value)
        {
            int index = FindIndex(typeId);
            if (index >= 0)
            {
                if (value == RoadTypePreference.Normal)
                {
                    typeIds.RemoveAt(index);
                    preferences.RemoveAt(index);
                }
                else
                {
                    preferences[index] = value;
                }
                return;
            }

            if (value != RoadTypePreference.Normal)
            {
                typeIds.Add(typeId);
                preferences.Add(value);
            }
        }

        private int FindIndex(int typeId)
        {
            for (int i = 0; i < typeIds.Count; i++)
            {
                if (typeIds[i] == typeId)
                {
                    return i;
                }
            }
            return -1;
        }

        public RoadTypePreference GetPreference(int typeId)
        {
            int index = FindIndex(typeId);
            if (index < 0)
            {
                return RoadTypePreference.Normal;
            }
            return preferences[index];
        }

        public float GetMultiplier(int typeId)
        {
            RoadTypePreference preference = GetPreference(typeId);
            if (preference == RoadTypePreference.Avoid)
            {
                return AvoidMultiplier;
            }
            if (preference == RoadTypePreference.Prefer)
            {
                return PreferMultiplier;
            }
            return 1f;
        }

        public void CopyFrom(RoutePreferences other)
        {
            Mode = other.Mode;
            UTurn = other.UTurn;
            AvoidMultiplier = other.AvoidMultiplier;
            PreferMultiplier = other.PreferMultiplier;

            typeIds.Clear();
            preferences.Clear();
            for (int i = 0; i < other.typeIds.Count; i++)
            {
                typeIds.Add(other.typeIds[i]);
                preferences.Add(other.preferences[i]);
            }
        }

        internal float GetSmallestMultiplier()
        {
            float smallest = 1f;
            for (int i = 0; i < typeIds.Count; i++)
            {
                float multiplier = GetMultiplier(typeIds[i]);
                if (multiplier < smallest)
                {
                    smallest = multiplier;
                }
            }
            return smallest;
        }
    }
}
