using UnityEngine;

namespace Gley.NavigationSystem
{
    internal readonly struct NavigationCommand
    {
        public NavigationCommandType Type { get; }
        public NavigationMap Map { get; }
        public Transform Car { get; }
        public MapMarker Marker { get; }
        public Vector3 Point { get; }
        public float FloatValue { get; }
        public int IntValue { get; }
        public int EnumValue { get; }

        public NavigationCommand(NavigationCommandType type, NavigationMap map, Transform car, Vector3 point, float floatValue, int intValue, int enumValue)
            : this(type, map, car, null, point, floatValue, intValue, enumValue)
        {
        }

        public NavigationCommand(NavigationCommandType type, MapMarker marker)
            : this(type, null, null, marker, Vector3.zero, 0f, 0, 0)
        {
        }

        public NavigationCommand(NavigationCommandType type, Vector3 point, MapMarker marker)
            : this(type, null, null, marker, point, 0f, 0, 0)
        {
        }

        private NavigationCommand(NavigationCommandType type, NavigationMap map, Transform car, MapMarker marker, Vector3 point, float floatValue, int intValue, int enumValue)
        {
            Type = type;
            Map = map;
            Car = car;
            Marker = marker;
            Point = point;
            FloatValue = floatValue;
            IntValue = intValue;
            EnumValue = enumValue;
        }
    }
}
