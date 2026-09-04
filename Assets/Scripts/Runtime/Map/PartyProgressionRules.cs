namespace Pachimon.Map
{
    public static class PartyProgressionRules
    {
        public const int MaxPartySize = 3;
        public const int StartCandidateCount = 3;
        public const int RivalCandidateCount = 6;
        public const int GangCandidateCount = 9;
        public const int FirstExpansionAfterRow = 10;
        public const int SecondExpansionAfterRow = 20;

        public static int GetPartySizeForRow(int rowIndex)
        {
            if (rowIndex <= FirstExpansionAfterRow)
            {
                return 1;
            }

            return rowIndex <= SecondExpansionAfterRow ? 2 : MaxPartySize;
        }
    }
}
