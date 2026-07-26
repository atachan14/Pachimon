using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Map;
using UnityEngine;

namespace Pachimon.UI
{
    public static class MapLayoutCalculator
    {
        public static MapLayout Calculate(
            RunMap runMap,
            int runSeed,
            Vector2 viewportSize,
            MapLayoutSettings settings)
        {
            if (runMap == null)
            {
                throw new ArgumentNullException(nameof(runMap));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var safeWidth = Mathf.Max(1f, viewportSize.x);
            var safeHeight = Mathf.Max(1f, viewportSize.y);
            var rowSpacing = safeHeight / settings.VisibleRowCount;
            var usableWidth = Mathf.Max(1f, safeWidth - (settings.HorizontalPadding * 2f));
            var columnSpacing = usableWidth / settings.MaxColumnCount;
            var positions = new Dictionary<string, Vector2>(runMap.Nodes.Count);
            var maximumRowIndex = 0;

            foreach (var row in runMap.Rows)
            {
                maximumRowIndex = Mathf.Max(maximumRowIndex, row.RowIndex);
                var nodeCount = row.NodeIds.Count;

                for (var index = 0; index < nodeCount; index++)
                {
                    var node = runMap.GetNode(row.NodeIds[index]);
                    if (node == null)
                    {
                        continue;
                    }

                    var centeredColumn = node.ColumnIndex - ((nodeCount - 1f) * 0.5f);
                    var baseX = (safeWidth * 0.5f) + (centeredColumn * columnSpacing);
                    var baseY = settings.VerticalPadding + (node.RowIndex * rowSpacing);
                    var jitter = GetDeterministicJitter(runSeed, node.NodeId);
                    var horizontalJitter = nodeCount > 1
                        ? jitter.x * columnSpacing * settings.HorizontalJitterRatio
                        : 0f;
                    var verticalJitter = node.NodeType != NodeType.Start
                        && node.NodeType != NodeType.Elite
                        ? jitter.y * rowSpacing * settings.VerticalJitterRatio
                        : 0f;
                    var edgePadding = settings.HorizontalPadding + (columnSpacing * 0.5f);
                    var x = Mathf.Clamp(baseX + horizontalJitter, edgePadding, safeWidth - edgePadding);

                    positions[node.NodeId] = new Vector2(x, baseY + verticalJitter);
                }
            }

            ApplyNodeGroupLayout(
                runMap,
                runSeed,
                safeWidth,
                rowSpacing,
                columnSpacing,
                settings,
                positions);

            var contentHeight = settings.VerticalPadding * 2f
                + ((maximumRowIndex + 1) * rowSpacing);
            return new MapLayout(
                new Vector2(safeWidth, contentHeight),
                rowSpacing,
                columnSpacing,
                positions);
        }

        private static void ApplyNodeGroupLayout(
            RunMap runMap,
            int runSeed,
            float safeWidth,
            float rowSpacing,
            float columnSpacing,
            MapLayoutSettings settings,
            IDictionary<string, Vector2> positions)
        {
            foreach (var group in runMap.NodeGroups.Values)
            {
                if (group.NodeType != NodeType.City || group.NodeIds.Count != 2)
                {
                    continue;
                }

                var members = group.NodeIds
                    .Select(runMap.GetNode)
                    .Where(node => node != null)
                    .OrderBy(node => node.ColumnIndex)
                    .ToArray();
                if (members.Length != 2 || members[0].RowIndex != members[1].RowIndex)
                {
                    continue;
                }

                var row = runMap.Rows[members[0].RowIndex];
                var centerColumn = ((members[0].ColumnIndex + members[1].ColumnIndex) * 0.5f)
                    - ((row.NodeIds.Count - 1f) * 0.5f);
                var jitter = GetDeterministicJitter(runSeed, group.GroupId);
                var portSpacing = columnSpacing * settings.CityPortSpacingRatio;
                var halfPortSpacing = portSpacing * 0.5f;
                var edgePadding = settings.HorizontalPadding + halfPortSpacing;
                var centerX = Mathf.Clamp(
                    (safeWidth * 0.5f)
                        + (centerColumn * columnSpacing)
                        + (jitter.x * columnSpacing * settings.HorizontalJitterRatio),
                    edgePadding,
                    safeWidth - edgePadding);
                var y = settings.VerticalPadding
                    + (members[0].RowIndex * rowSpacing)
                    + (jitter.y * rowSpacing * settings.VerticalJitterRatio);

                positions[members[0].NodeId] = new Vector2(centerX - halfPortSpacing, y);
                positions[members[1].NodeId] = new Vector2(centerX + halfPortSpacing, y);
            }
        }

        private static Vector2 GetDeterministicJitter(int runSeed, string nodeId)
        {
            unchecked
            {
                var hash = 2166136261u ^ (uint)runSeed;

                for (var index = 0; index < nodeId.Length; index++)
                {
                    hash ^= nodeId[index];
                    hash *= 16777619u;
                }

                var xState = NextState(hash);
                var yState = NextState(xState);
                return new Vector2(ToSignedUnit(xState), ToSignedUnit(yState));
            }
        }

        private static uint NextState(uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        private static float ToSignedUnit(uint state)
        {
            return ((state & 0x00FFFFFFu) / 8388607.5f) - 1f;
        }
    }
}
