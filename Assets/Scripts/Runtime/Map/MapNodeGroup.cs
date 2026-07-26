using System.Collections.Generic;

namespace Pachimon.Map
{
    public sealed class MapNodeGroup
    {
        private readonly List<string> _nodeIds;

        public MapNodeGroup(string groupId, NodeType nodeType, IEnumerable<string> nodeIds)
        {
            GroupId = groupId;
            NodeType = nodeType;
            _nodeIds = new List<string>(nodeIds);
        }

        public string GroupId { get; }
        public NodeType NodeType { get; }
        public IReadOnlyList<string> NodeIds => _nodeIds;
    }
}
