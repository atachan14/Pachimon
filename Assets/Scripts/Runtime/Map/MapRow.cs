using System.Collections.Generic;

namespace Pachimon.Map
{
    public sealed class MapRow
    {
        public MapRow(int rowIndex)
        {
            RowIndex = rowIndex;
        }

        public int RowIndex { get; }

        public List<string> NodeIds { get; } = new();
    }
}
