using System.Collections.Generic;

namespace Pachimon.Map
{
    public sealed class RunMap
    {
        public Dictionary<string, MapNode> Nodes { get; } = new();

        public List<MapRow> Rows { get; } = new();

        public string StartNodeId { get; set; }

        public MapNode GetNode(string nodeId)
        {
            return nodeId != null && Nodes.TryGetValue(nodeId, out var node) ? node : null;
        }
    }
}
