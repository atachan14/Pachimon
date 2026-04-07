namespace Pachimon.Map
{
    public abstract class NodeContent
    {
    }

    public sealed class StartNodeContent : NodeContent
    {
        public StartNodeContent(string[] candidatePachimonIds, int selectionCount)
        {
            CandidatePachimonIds = candidatePachimonIds;
            SelectionCount = selectionCount;
        }

        public string[] CandidatePachimonIds { get; }

        public int SelectionCount { get; }
    }

    public sealed class BattleNodeContent : NodeContent
    {
        public BattleNodeContent(int enemyPartySeed, int goldReward)
        {
            EnemyPartySeed = enemyPartySeed;
            GoldReward = goldReward;
        }

        public int EnemyPartySeed { get; }

        public int GoldReward { get; }
    }

    public sealed class RestSpotNodeContent : NodeContent
    {
        public RestSpotNodeContent(int healValue)
        {
            HealValue = healValue;
        }

        public int HealValue { get; }
    }

    public sealed class CityNodeContent : NodeContent
    {
        public CityNodeContent(int shopSeed)
        {
            ShopSeed = shopSeed;
        }

        public int ShopSeed { get; }
    }

    public sealed class LeagueGateNodeContent : NodeContent
    {
        public LeagueGateNodeContent(int requiredBadgeCount, string failureMode)
        {
            RequiredBadgeCount = requiredBadgeCount;
            FailureMode = failureMode;
        }

        public int RequiredBadgeCount { get; }

        public string FailureMode { get; }
    }
}
