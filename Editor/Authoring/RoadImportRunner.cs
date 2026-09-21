using System.Collections.Generic;

namespace Gley.NavigationSystem.Editor
{
    public class RoadImportRunner
    {
        private readonly IGroundProbe probe;
        private readonly float maxDeviation;
        private readonly float maxSpacing;
        private readonly float mergeDistance;

        public RoadImportRunner(IGroundProbe probe, float maxDeviation, float maxSpacing, float mergeDistance)
        {
            this.probe = probe;
            this.maxDeviation = maxDeviation;
            this.maxSpacing = maxSpacing;
            this.mergeDistance = mergeDistance;
        }

        public int CountModifiedImportedRoads(RoadNetworkAuthoring asset, string sourceTag)
        {
            int count = 0;
            List<AuthoringRoad> roads = asset.Roads;
            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                if (road.SourceTag == sourceTag && road.ModifiedAfterImport)
                {
                    count++;
                }
            }
            return count;
        }

        public void Run(IRoadImporter importer, RoadNetworkAuthoring asset)
        {
            RoadEditOperations operations = new RoadEditOperations(asset, probe, maxDeviation, maxSpacing);

            DeleteRoadsWithSourceTag(asset, operations, importer.SourceTag);

            RoadImportContext context = new RoadImportContext(asset, operations, importer.SourceTag, mergeDistance);
            importer.Import(context);
        }

        private void DeleteRoadsWithSourceTag(RoadNetworkAuthoring asset, RoadEditOperations operations, string sourceTag)
        {
            List<AuthoringRoad> roads = asset.Roads;
            List<int> roadIdsToDelete = new List<int>();
            for (int i = 0; i < roads.Count; i++)
            {
                if (roads[i].SourceTag == sourceTag)
                {
                    roadIdsToDelete.Add(roads[i].Id);
                }
            }

            for (int i = 0; i < roadIdsToDelete.Count; i++)
            {
                operations.DeleteRoad(roadIdsToDelete[i]);
            }
        }
    }
}
