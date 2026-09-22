using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class MarkerGrid
    {
        public const float CellSize = 100f;

        private readonly Dictionary<long, List<int>> cells = new Dictionary<long, List<int>>();

        public long ComputeCell(Vector3 truePosition)
        {
            int cellX = Mathf.FloorToInt(truePosition.x / CellSize);
            int cellZ = Mathf.FloorToInt(truePosition.z / CellSize);
            return PackCell(cellX, cellZ);
        }

        private long PackCell(int cellX, int cellZ)
        {
            return ((long)cellX << 32) | (uint)cellZ;
        }

        public void Add(int entryIndex, long cell)
        {
            List<int> list;
            if (!cells.TryGetValue(cell, out list))
            {
                list = new List<int>();
                cells.Add(cell, list);
            }
            list.Add(entryIndex);
        }

        public void Remove(int entryIndex, long cell)
        {
            List<int> list;
            if (!cells.TryGetValue(cell, out list))
            {
                return;
            }
            list.Remove(entryIndex);
        }

        public void Move(int entryIndex, long oldCell, long newCell)
        {
            if (oldCell == newCell)
            {
                return;
            }
            Remove(entryIndex, oldCell);
            Add(entryIndex, newCell);
        }

        public void QueryArea(Vector2 minXZ, Vector2 maxXZ, List<int> output)
        {
            output.Clear();

            int cellXMin = Mathf.FloorToInt(minXZ.x / CellSize);
            int cellXMax = Mathf.FloorToInt(maxXZ.x / CellSize);
            int cellZMin = Mathf.FloorToInt(minXZ.y / CellSize);
            int cellZMax = Mathf.FloorToInt(maxXZ.y / CellSize);

            for (int cz = cellZMin; cz <= cellZMax; cz++)
            {
                for (int cx = cellXMin; cx <= cellXMax; cx++)
                {
                    List<int> list;
                    if (!cells.TryGetValue(PackCell(cx, cz), out list))
                    {
                        continue;
                    }
                    for (int i = 0; i < list.Count; i++)
                    {
                        output.Add(list[i]);
                    }
                }
            }
        }
    }
}
