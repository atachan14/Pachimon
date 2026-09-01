namespace Pachimon.Map
{
    public sealed class MapGenerationSettings
    {
        public int MainRowStart { get; } = 1;

        public int MainRowEnd { get; } = 35;

        public int AdventureRowStart { get; } = 3;

        public int LeagueGateRow { get; } = 36;

        public int EliteRowStart { get; } = 37;

        public int EliteRowEnd { get; } = 40;

        public int HallOfFameRow { get; } = 41;

        public int BaseNodesPerRow { get; } = 3;

        public int MaxNodesPerRow { get; } = 6;

        public int MainNodeCount { get; } = 149;

        public int GymNodeCount { get; } = 24;

        public int RestSpotNodeCount { get; } = 24;

        public int EventNodeCount { get; } = 16;

        public int RequiredBadgeCount { get; } = 8;

        public int PlacementAttemptLimit { get; } = 1000;

        public int EarlyRandomSkillCount { get; } = 2;

        public int MidRandomSkillStartRow { get; } = 18;

        public int MidRandomSkillCount { get; } = 3;

        public int LateRandomSkillStartRow { get; } = 27;

        public int LateRandomSkillCount { get; } = 4;

        public int GymMatchingSkillCount { get; } = 2;

        public int EliteMatchingSkillCount { get; } = 3;

        public int RestSpotHealPercent { get; } = 50;

        public double AdditionalEdgeChance { get; } = 0.5;

        public double MaximumAdditionalEdgeDistance { get; } = 0.4;

        public int[] CityRows { get; } = { 4, 8, 12, 16, 20, 24, 28, 32 };
    }
}
