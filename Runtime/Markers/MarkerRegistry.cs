using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class MarkerRegistry
    {
        private const float MinHeadingLength = 0.0001f;

        private readonly List<MarkerEntry> entries = new List<MarkerEntry>();
        private readonly Dictionary<MapMarker, int> markerToIndex = new Dictionary<MapMarker, int>();
        private readonly Stack<int> freeIndices = new Stack<int>();
        private readonly List<int> queryScratch = new List<int>();
        private readonly MarkerGrid grid = new MarkerGrid();

        private int playerIndex = -1;

        public int PlayerIndex { get { return playerIndex; } }
        public int EntryCount { get { return entries.Count; } }

        public void AddObject(MapMarker marker)
        {
            if (markerToIndex.ContainsKey(marker))
            {
                return;
            }

            int index = AcquireIndex();
            MarkerEntry entry = entries[index];
            entry.Kind = MarkerEntryKind.Object;
            entry.Marker = marker;
            entry.Transform = marker.transform;
            entry.Prefab = marker.Prefab;
            entry.RotationMode = marker.RotationMode;
            entry.ChannelMask = marker.ChannelMask;
            entry.IsStatic = marker.IsStatic;
            entry.CanBeDestination = marker.CanBeDestination;
            entry.ShowArrow = marker.ShowOffScreenArrow;
            entry.IsPlayer = false;
            entry.Alive = true;
            entry.Initialized = false;
            entry.TruePosition = Vector3.zero;
            entry.TrueHeading = Vector3.zero;
            entry.GridCell = grid.ComputeCell(Vector3.zero);
            grid.Add(index, entry.GridCell);

            markerToIndex.Add(marker, index);
        }

        private int AcquireIndex()
        {
            if (freeIndices.Count > 0)
            {
                return freeIndices.Pop();
            }

            entries.Add(new MarkerEntry());
            return entries.Count - 1;
        }

        public void RemoveObject(MapMarker marker)
        {
            int index;
            if (!markerToIndex.TryGetValue(marker, out index))
            {
                return;
            }
            markerToIndex.Remove(marker);
            ReleaseEntry(index);
        }

        private void ReleaseEntry(int index)
        {
            MarkerEntry entry = entries[index];
            grid.Remove(index, entry.GridCell);
            entry.Alive = false;
            entry.Marker = null;
            entry.Transform = null;
            entry.Prefab = null;
            freeIndices.Push(index);
        }

        public int AddPoint(Vector3 truePos, GameObject prefab, int channelMask, bool showArrow)
        {
            int index = AcquireIndex();
            MarkerEntry entry = entries[index];
            entry.Kind = MarkerEntryKind.Point;
            entry.Marker = null;
            entry.Transform = null;
            entry.Prefab = prefab;
            entry.RotationMode = MarkerRotationMode.Upright;
            entry.ChannelMask = channelMask;
            entry.IsStatic = true;
            entry.CanBeDestination = false;
            entry.ShowArrow = showArrow;
            entry.IsPlayer = false;
            entry.Alive = true;
            entry.Initialized = true;
            entry.TruePosition = truePos;
            entry.TrueHeading = Vector3.zero;
            entry.GridCell = grid.ComputeCell(truePos);
            grid.Add(index, entry.GridCell);
            return index;
        }

        public void RemovePoint(int index)
        {
            if (index < 0 || index >= entries.Count || !entries[index].Alive)
            {
                return;
            }
            ReleaseEntry(index);
        }

        public void SetPointPosition(int index, Vector3 truePos)
        {
            MarkerEntry entry = entries[index];
            long newCell = grid.ComputeCell(truePos);
            grid.Move(index, entry.GridCell, newCell);
            entry.GridCell = newCell;
            entry.TruePosition = truePos;
        }

        public void EnsurePlayer(GameObject prefab, int channelMask)
        {
            if (playerIndex >= 0)
            {
                return;
            }

            playerIndex = AcquireIndex();
            MarkerEntry entry = entries[playerIndex];
            entry.Kind = MarkerEntryKind.Object;
            entry.Marker = null;
            entry.Transform = null;
            entry.Prefab = prefab;
            entry.RotationMode = MarkerRotationMode.FollowHeading;
            entry.ChannelMask = channelMask;
            entry.IsStatic = false;
            entry.CanBeDestination = false;
            entry.ShowArrow = false;
            entry.IsPlayer = true;
            entry.Alive = true;
            entry.Initialized = true;
            entry.TruePosition = Vector3.zero;
            entry.TrueHeading = Vector3.forward;
            entry.GridCell = grid.ComputeCell(Vector3.zero);
            grid.Add(playerIndex, entry.GridCell);
        }

        public void SetPlayer(Vector3 truePos, Vector3 trueHeading)
        {
            if (playerIndex < 0)
            {
                return;
            }

            MarkerEntry entry = entries[playerIndex];
            long newCell = grid.ComputeCell(truePos);
            grid.Move(playerIndex, entry.GridCell, newCell);
            entry.GridCell = newCell;
            entry.TruePosition = truePos;
            entry.TrueHeading = trueHeading;
        }

        public void UpdateMarkerRegistryLogic(WorldConverter converter)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                MarkerEntry entry = entries[i];
                if (!entry.Alive || entry.Kind != MarkerEntryKind.Object || entry.IsPlayer)
                {
                    continue;
                }
                if (entry.IsStatic && entry.Initialized)
                {
                    continue;
                }

                ReadObjectMarkerTransform(entry, converter);
                entry.Initialized = true;

                long newCell = grid.ComputeCell(entry.TruePosition);
                if (newCell != entry.GridCell)
                {
                    grid.Move(i, entry.GridCell, newCell);
                    entry.GridCell = newCell;
                }
            }
        }

        private void ReadObjectMarkerTransform(MarkerEntry entry, WorldConverter converter)
        {
            entry.TruePosition = converter.WorldToTrue(entry.Transform.position);

            Vector3 forward = entry.Transform.forward;
            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
            if (flatForward.sqrMagnitude >= MinHeadingLength)
            {
                entry.TrueHeading = converter.WorldDirectionToTrue(flatForward);
            }
        }

        public void QueryVisible(Vector2 minXZ, Vector2 maxXZ, int channelMask, List<int> output)
        {
            output.Clear();
            grid.QueryArea(minXZ, maxXZ, queryScratch);

            for (int i = 0; i < queryScratch.Count; i++)
            {
                int index = queryScratch[i];
                MarkerEntry entry = entries[index];
                if (!entry.Alive || (entry.ChannelMask & channelMask) == 0)
                {
                    continue;
                }
                output.Add(index);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                MarkerEntry entry = entries[i];
                if (!entry.Alive || !entry.ShowArrow || (entry.ChannelMask & channelMask) == 0)
                {
                    continue;
                }
                if (output.Contains(i))
                {
                    continue;
                }
                output.Add(i);
            }
        }

        public MarkerEntry GetEntry(int index)
        {
            return entries[index];
        }
    }
}
