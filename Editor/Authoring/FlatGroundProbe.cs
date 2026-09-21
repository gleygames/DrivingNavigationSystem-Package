using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class FlatGroundProbe : IGroundProbe
    {
        private readonly float y;

        public FlatGroundProbe(float y)
        {
            this.y = y;
        }

        public bool Probe(Vector3 truePos, out float trueY)
        {
            trueY = y;
            return true;
        }
    }
}
