using System.Collections.Generic;

namespace Pachimon.Map
{
    public sealed class RunMap
    {
        private readonly Dictionary<string, string> _groupIdsByNodeId = new();

        public Dictionary<string, MapNode> Nodes { get; } = new();

        public Dictionary<string, MapNodeGroup> NodeGroups { get; } = new();

        public List<MapRow> Rows { get; } = new();

        public string StartNodeId { get; set; }

        public MapNode GetNode(string nodeId)
        {
            return nodeId != null && Nodes.TryGetValue(nodeId, out var node) ? node : null;
        }

        public MapNodeGroup GetNodeGroup(string groupId)
        {
            return groupId != null && NodeGroups.TryGetValue(groupId, out var group) ? group : null;
        }

        public MapNodeGroup GetNodeGroupForNode(string nodeId)
        {
            return nodeId != null
                && _groupIdsByNodeId.TryGetValue(nodeId, out var groupId)
                ? GetNodeGroup(groupId)
                : null;
        }

        public void AddNodeGroup(MapNodeGroup group)
        {
            NodeGroups.Add(group.GroupId, group);
            foreach (var nodeId in group.NodeIds)
            {
                _groupIdsByNodeId.Add(nodeId, group.GroupId);
            }
        }
    }
}
