using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class PhysicsGroundProbe : IGroundProbe
    {
        private const float ProbeHeightAbove = 2f;
        private const float ProbeMaxDistance = 5f;

        private readonly WorldConverter converter;
        private readonly LayerMask roadLayers;

        public PhysicsGroundProbe(LayerMask roadLayers, float unitsPerMeter)
        {
            this.roadLayers = roadLayers;
            converter = new WorldConverter();
            converter.SetUnitsPerMeter(unitsPerMeter);
        }

        public bool Probe(Vector3 truePos, out float trueY)
        {
            Vector3 originTrue = truePos + new Vector3(0f, ProbeHeightAbove, 0f);
            Vector3 origin = converter.TrueToWorld(originTrue);
            float maxDistanceWorld = ProbeMaxDistance * converter.UnitsPerMeter;

            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, Vector3.down, out hit, maxDistanceWorld, roadLayers, QueryTriggerInteraction.Ignore);
            if (didHit)
            {
                trueY = converter.WorldToTrue(hit.point).y;
                return true;
            }

            trueY = truePos.y;
            return false;
        }
    }
}
