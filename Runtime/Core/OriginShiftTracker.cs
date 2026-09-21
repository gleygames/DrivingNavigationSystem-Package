using UnityEngine;

namespace Gley.NavigationSystem
{
    public class OriginShiftTracker
    {
        private Vector3 shift;

        public ShiftSource Mode { get; }

        public Vector3 Shift { get { return shift; } }

        public OriginShiftTracker(ShiftSource mode)
        {
            Mode = mode;
            shift = Vector3.zero;
        }

        public void UpdateFromRectangle(Vector3 currentWorldPosition, Vector3 storedEditTimeWorldPosition)
        {
            shift = currentWorldPosition - storedEditTimeWorldPosition;
        }

        public void AddManualDelta(Vector3 delta)
        {
            shift += delta;
        }

        public void ResetForNewMap()
        {
        }
    }
}
