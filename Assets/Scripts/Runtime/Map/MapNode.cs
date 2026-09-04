using System.Collections.Generic;

namespace Pachimon.Map
{
    public sealed class MapNode
    {
        public MapNode(
            string nodeId,
            int rowIndex,
            int columnIndex,
            NodeType nodeType,
            NodeContent content,
            float? displayRowPosition = null)
        {
            NodeId = nodeId;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            NodeType = nodeType;
            Content = content;
            DisplayRowPosition = displayRowPosition ?? rowIndex;
        }

        public string NodeId { get; }

        public int RowIndex { get; }

        public int ColumnIndex { get; }

        public NodeType NodeType { get; }

        public NodeContent Content { get; }

        public float DisplayRowPosition { get; }

        public List<string> NextNodeIds { get; } = new();

    }
}
