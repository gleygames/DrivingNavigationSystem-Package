using System.Collections.Generic;

namespace Gley.NavigationSystem.Editor
{
    public class RoadTypeDeletion
    {
        public int CountUsage(int typeId, List<RoadNetworkAuthoring> assets)
        {
            int count = 0;
            for (int i = 0; i < assets.Count; i++)
            {
                RoadNetworkAuthoring asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                List<AuthoringRoad> roads = asset.Roads;
                for (int j = 0; j < roads.Count; j++)
                {
                    if (roads[j].TypeId == typeId)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public void Reassign(int fromTypeId, int toTypeId, List<RoadNetworkAuthoring> assets)
        {
            for (int i = 0; i < assets.Count; i++)
            {
                RoadNetworkAuthoring asset = assets[i];
                if (asset == null)
                {
                    continue;
                }

                bool changed = false;
                List<AuthoringRoad> roads = asset.Roads;
                for (int j = 0; j < roads.Count; j++)
                {
                    if (roads[j].TypeId == fromTypeId)
                    {
                        roads[j].SetTypeId(toTypeId);
                        changed = true;
                    }
                }

                if (changed)
                {
                    asset.MarkChanged();
                }
            }
        }
    }
}
