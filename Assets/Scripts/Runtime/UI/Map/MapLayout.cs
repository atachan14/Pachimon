using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class MapLayout
    {
        private readonly Dictionary<string, Vector2> _nodePositions;

        public MapLayout(
            Vector2 contentSize,
            float rowSpacing,
            float columnSpacing,
            float nodeSize,
            Dictionary<string, Vector2> nodePositions)
        {
            ContentSize = contentSize;
            RowSpacing = rowSpacing;
            ColumnSpacing = columnSpacing;
            NodeSize = nodeSize;
            _nodePositions = nodePositions;
        }

        public Vector2 ContentSize { get; }
        public float RowSpacing { get; }
        public float ColumnSpacing { get; }
        public float NodeSize { get; }
        public IReadOnlyDictionary<string, Vector2> NodePositions => _nodePositions;

        public bool TryGetNodePosition(string nodeId, out Vector2 position)
        {
            if (nodeId != null)
            {
                return _nodePositions.TryGetValue(nodeId, out position);
            }

            position = default;
            return false;
        }
    }
}
