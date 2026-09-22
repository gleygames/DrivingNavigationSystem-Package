using UnityEngine;

namespace Gley.NavigationSystem
{
    internal readonly struct NavigationCommand
    {
        public NavigationCommandType Type { get; }
        public NavigationMap Map { get; }
        public Transform Car { get; }
        public Vector3 Point { get; }
        public float FloatValue { get; }
        public int IntValue { get; }
        public int EnumValue { get; }

        public NavigationCommand(NavigationCommandType type, NavigationMap map, Transform car, Vector3 point, float floatValue, int intValue, int enumValue)
        {
            Type = type;
            Map = map;
            Car = car;
            Point = point;
            FloatValue = floatValue;
            IntValue = intValue;
            EnumValue = enumValue;
        }
    }
}
