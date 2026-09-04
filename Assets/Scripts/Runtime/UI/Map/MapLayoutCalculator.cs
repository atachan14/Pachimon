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
            LayoutMode layoutMode,
            float screenAspectRatio,
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
            var nodeSize = settings.GetNodeSize(
                layoutMode,
                safeWidth,
                screenAspectRatio);
            var usesPortraitCompactSpacing = layoutMode == LayoutMode.Compact
                && screenAspectRatio < 1f;
            var rowSpacing = usesPortraitCompactSpacing
                ? settings.CompactRowSpacing
                : safeHeight / settings.VisibleRowCount;
            var verticalPadding = Mathf.Max(
                settings.VerticalPadding,
                (nodeSize * 0.6f) + settings.NodeEdgeGap);
            var edgePadding = Mathf.Min(
                settings.HorizontalPadding + (nodeSize * 0.57f),
                safeWidth * 0.5f);
            var usableWidth = Mathf.Max(0f, safeWidth - (edgePadding * 2f));
            var columnSpacing = usableWidth / Mathf.Max(1, settings.MaxColumnCount - 1);
            var positions = new Dictionary<string, Vector2>(runMap.Nodes.Count);
            var encounterRows = runMap.Nodes.Values
                .Where(node => node.NodeType == NodeType.PartyEncounter)
                .Select(node => Mathf.FloorToInt(node.DisplayRowPosition))
                .Distinct()
                .OrderBy(row => row)
                .ToArray();
            var maximumDisplayRow = 0f;

            foreach (var row in runMap.Rows)
            {
                var displayRow = GetRegularDisplayRow(
                    row.RowIndex,
                    encounterRows,
                    settings.PartyEncounterGapRows);
                maximumDisplayRow = Mathf.Max(maximumDisplayRow, displayRow);
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
                    var baseY = verticalPadding + (displayRow * rowSpacing);
                    var jitter = GetDeterministicJitter(runSeed, node.NodeId);
                    var horizontalJitter = nodeCount > 1
                        ? jitter.x * columnSpacing * settings.HorizontalJitterRatio
                        : 0f;
                    var verticalJitter = node.NodeType != NodeType.Start
                        && node.NodeType != NodeType.Elite
                        && node.NodeType != NodeType.HallOfFame
                        ? jitter.y * rowSpacing * settings.VerticalJitterRatio
                        : 0f;
                    var x = Mathf.Clamp(baseX + horizontalJitter, edgePadding, safeWidth - edgePadding);

                    positions[node.NodeId] = new Vector2(x, baseY + verticalJitter);
                }
            }

            foreach (var node in runMap.Nodes.Values.Where(node => !positions.ContainsKey(node.NodeId)))
            {
                var displayRow = GetEncounterDisplayRow(
                    node.DisplayRowPosition,
                    encounterRows,
                    settings.PartyEncounterGapRows);
                maximumDisplayRow = Mathf.Max(maximumDisplayRow, displayRow);
                positions[node.NodeId] = new Vector2(
                    safeWidth - edgePadding,
                    verticalPadding + (displayRow * rowSpacing));
            }

            ApplyNodeGroupLayout(
                runMap,
                runSeed,
                safeWidth,
                rowSpacing,
                columnSpacing,
                verticalPadding,
                encounterRows,
                settings,
                positions);

            var contentHeight = verticalPadding * 2f
                + (maximumDisplayRow * rowSpacing);
            return new MapLayout(
                new Vector2(safeWidth, contentHeight),
                rowSpacing,
                columnSpacing,
                nodeSize,
                positions);
        }

        private static float GetRegularDisplayRow(
            int rowIndex,
            IReadOnlyList<int> encounterRows,
            float gapRows)
        {
            return rowIndex + (encounterRows.Count(row => row < rowIndex) * gapRows);
        }

        private static float GetEncounterDisplayRow(
            float originalDisplayRow,
            IReadOnlyList<int> encounterRows,
            float gapRows)
        {
            var precedingRow = Mathf.FloorToInt(originalDisplayRow);
            var earlierEncounterCount = encounterRows.Count(row => row < precedingRow);
            return originalDisplayRow
                + (earlierEncounterCount * gapRows)
                + (gapRows * 0.5f);
        }

        private static void ApplyNodeGroupLayout(
            RunMap runMap,
            int runSeed,
            float safeWidth,
            float rowSpacing,
            float columnSpacing,
            float verticalPadding,
            IReadOnlyList<int> encounterRows,
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
                var y = verticalPadding
                    + (GetRegularDisplayRow(
                        members[0].RowIndex,
                        encounterRows,
                        settings.PartyEncounterGapRows) * rowSpacing)
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
