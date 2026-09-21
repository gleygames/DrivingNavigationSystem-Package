using Gley.Common;
using UnityEngine;

namespace Gley.NavigationSystem
{
    public class WorldConverter
    {
        private Vector3 shift;
        private float unitsPerMeter;

        public Vector3 Shift { get { return shift; } }
        public float UnitsPerMeter { get { return unitsPerMeter; } }

        public WorldConverter()
        {
            shift = Vector3.zero;
            unitsPerMeter = 1f;
        }

        public Vector3 WorldToTrue(Vector3 world)
        {
            return (world - shift) / unitsPerMeter;
        }

        public Vector3 TrueToWorld(Vector3 truePos)
        {
            return truePos * unitsPerMeter + shift;
        }

        public Vector3 WorldDirectionToTrue(Vector3 dir)
        {
            return dir.normalized;
        }

        public float WorldDistanceToTrue(float distance)
        {
            return distance / unitsPerMeter;
        }

        public void SetShift(Vector3 value)
        {
            shift = value;
        }

        public void SetUnitsPerMeter(float value)
        {
            if (value <= 0f)
            {
                CustomLogger.LogError("WorldConverter: unitsPerMeter must be greater than zero. Keeping previous value " + unitsPerMeter + ".");
                return;
            }
            unitsPerMeter = value;
        }
    }
}
