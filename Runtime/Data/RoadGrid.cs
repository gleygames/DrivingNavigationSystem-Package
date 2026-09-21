using UnityEngine;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public class RoadGrid
    {
        public const float DefaultCellSize = 50f;

        [SerializeField] private int[] cellStart = new int[0];
        [SerializeField] private int[] cellCount = new int[0];
        [SerializeField] private int[] entries = new int[0];
        [SerializeField] private int[] entryRoad = new int[0];
        [SerializeField] private Vector2 origin;
        [SerializeField] private float cellSize = DefaultCellSize;
        [SerializeField] private int cellsX;
        [SerializeField] private int cellsZ;

        public Vector2 Origin { get { return origin; } }
        public float CellSize { get { return cellSize; } }
        public int CellsX { get { return cellsX; } }
        public int CellsZ { get { return cellsZ; } }

        public int GetCellStart(int cellIndex)
        {
            return cellStart[cellIndex];
        }

        public int GetCellCount(int cellIndex)
        {
            return cellCount[cellIndex];
        }

        public int GetEntryPoint(int entryIndex)
        {
            return entries[entryIndex];
        }

        public int GetEntryRoad(int entryIndex)
        {
            return entryRoad[entryIndex];
        }

        public int GetCellIndex(int cellX, int cellZ)
        {
            return cellZ * cellsX + cellX;
        }

        public void GetCellRange(float minX, float minZ, float maxX, float maxZ, out int cellXMin, out int cellXMax, out int cellZMin, out int cellZMax)
        {
            cellXMin = ClampCellCoord(Mathf.FloorToInt((minX - origin.x) / cellSize), cellsX);
            cellXMax = ClampCellCoord(Mathf.FloorToInt((maxX - origin.x) / cellSize), cellsX);
            cellZMin = ClampCellCoord(Mathf.FloorToInt((minZ - origin.y) / cellSize), cellsZ);
            cellZMax = ClampCellCoord(Mathf.FloorToInt((maxZ - origin.y) / cellSize), cellsZ);
        }

        private int ClampCellCoord(int cellCoord, int cellCountOnAxis)
        {
            if (cellCoord < 0)
            {
                return 0;
            }
            if (cellCoord > cellCountOnAxis - 1)
            {
                return cellCountOnAxis - 1;
            }
            return cellCoord;
        }

        internal void Build(RoadRecord[] roads, Vector3[] points, float buildCellSize)
        {
            cellSize = buildCellSize;

            if (points.Length == 0)
            {
                origin = Vector2.zero;
                cellsX = 1;
                cellsZ = 1;
                cellStart = new int[1];
                cellCount = new int[1];
                entries = new int[0];
                entryRoad = new int[0];
                return;
            }

            float minX = points[0].x;
            float maxX = points[0].x;
            float minZ = points[0].z;
            float maxZ = points[0].z;
            for (int i = 1; i < points.Length; i++)
            {
                Vector3 point = points[i];
                if (point.x < minX)
                {
                    minX = point.x;
                }
                if (point.x > maxX)
                {
                    maxX = point.x;
                }
                if (point.z < minZ)
                {
                    minZ = point.z;
                }
                if (point.z > maxZ)
                {
                    maxZ = point.z;
                }
            }

            origin = new Vector2(minX, minZ);
            cellsX = Mathf.Max(1, Mathf.CeilToInt((maxX - minX) / cellSize));
            cellsZ = Mathf.Max(1, Mathf.CeilToInt((maxZ - minZ) / cellSize));

            int cellTotal = cellsX * cellsZ;
            int[] counts = new int[cellTotal];

            for (int r = 0; r < roads.Length; r++)
            {
                RoadRecord road = roads[r];
                for (int p = 0; p < road.PointCount - 1; p++)
                {
                    int pointIndex = road.FirstPoint + p;
                    AddSegmentToCounts(points[pointIndex], points[pointIndex + 1], counts);
                }
            }

            cellStart = new int[cellTotal];
            int runningTotal = 0;
            for (int c = 0; c < cellTotal; c++)
            {
                cellStart[c] = runningTotal;
                runningTotal += counts[c];
            }

            cellCount = counts;

            entries = new int[runningTotal];
            entryRoad = new int[runningTotal];
            int[] writeCursor = new int[cellTotal];
            for (int c = 0; c < cellTotal; c++)
            {
                writeCursor[c] = cellStart[c];
            }

            for (int r = 0; r < roads.Length; r++)
            {
                RoadRecord road = roads[r];
                for (int p = 0; p < road.PointCount - 1; p++)
                {
                    int pointIndex = road.FirstPoint + p;
                    WriteSegmentEntries(points[pointIndex], points[pointIndex + 1], pointIndex, r, writeCursor);
                }
            }
        }

        private void AddSegmentToCounts(Vector3 start, Vector3 end, int[] counts)
        {
            int cellXMin;
            int cellXMax;
            int cellZMin;
            int cellZMax;
            GetSegmentCellRange(start, end, out cellXMin, out cellXMax, out cellZMin, out cellZMax);

            for (int cz = cellZMin; cz <= cellZMax; cz++)
            {
                for (int cx = cellXMin; cx <= cellXMax; cx++)
                {
                    counts[GetCellIndex(cx, cz)]++;
                }
            }
        }

        private void WriteSegmentEntries(Vector3 start, Vector3 end, int pointIndex, int roadIndex, int[] writeCursor)
        {
            int cellXMin;
            int cellXMax;
            int cellZMin;
            int cellZMax;
            GetSegmentCellRange(start, end, out cellXMin, out cellXMax, out cellZMin, out cellZMax);

            for (int cz = cellZMin; cz <= cellZMax; cz++)
            {
                for (int cx = cellXMin; cx <= cellXMax; cx++)
                {
                    int cellIndex = GetCellIndex(cx, cz);
                    int writeIndex = writeCursor[cellIndex];
                    entries[writeIndex] = pointIndex;
                    entryRoad[writeIndex] = roadIndex;
                    writeCursor[cellIndex] = writeIndex + 1;
                }
            }
        }

        private void GetSegmentCellRange(Vector3 start, Vector3 end, out int cellXMin, out int cellXMax, out int cellZMin, out int cellZMax)
        {
            float minX = start.x;
            float maxX = start.x;
            float minZ = start.z;
            float maxZ = start.z;
            if (end.x < minX)
            {
                minX = end.x;
            }
            if (end.x > maxX)
            {
                maxX = end.x;
            }
            if (end.z < minZ)
            {
                minZ = end.z;
            }
            if (end.z > maxZ)
            {
                maxZ = end.z;
            }

            GetCellRange(minX, minZ, maxX, maxZ, out cellXMin, out cellXMax, out cellZMin, out cellZMax);
        }
    }
}
