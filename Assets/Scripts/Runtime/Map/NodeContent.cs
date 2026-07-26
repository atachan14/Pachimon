using Pachimon.Reward;
using Pachimon.Trainer;

namespace Pachimon.Map
{
    public abstract class NodeContent
    {
    }

    public sealed class StartNodeContent : NodeContent
    {
        public StartNodeContent(string[] candidatePachimonInstanceIds, int selectionCount)
        {
            CandidatePachimonInstanceIds = candidatePachimonInstanceIds;
            SelectionCount = selectionCount;
        }

        public string[] CandidatePachimonInstanceIds { get; }

        public int SelectionCount { get; }
    }

    public sealed class BattleNodeContent : NodeContent
    {
        public BattleNodeContent(
            string[] enemyPachimonInstanceIds,
            NodeReward nodeReward,
            TrainerProfile trainerProfile)
        {
            EnemyPachimonInstanceIds = enemyPachimonInstanceIds;
            NodeReward = nodeReward;
            TrainerProfile = trainerProfile;
        }

        public string[] EnemyPachimonInstanceIds { get; }

        public NodeReward NodeReward { get; }

        public TrainerProfile TrainerProfile { get; }
    }

    public sealed class GymNodeContent : NodeContent
    {
        public GymNodeContent(
            string[] enemyPachimonInstanceIds,
            NodeReward nodeReward,
            TrainerProfile trainerProfile)
        {
            EnemyPachimonInstanceIds = enemyPachimonInstanceIds;
            NodeReward = nodeReward;
            TrainerProfile = trainerProfile;
        }

        public string[] EnemyPachimonInstanceIds { get; }

        public NodeReward NodeReward { get; }

        public TrainerProfile TrainerProfile { get; }
    }

    public sealed class EliteNodeContent : NodeContent
    {
        public EliteNodeContent(
            string[] enemyPachimonInstanceIds,
            TrainerProfile trainerProfile)
        {
            EnemyPachimonInstanceIds = enemyPachimonInstanceIds;
            TrainerProfile = trainerProfile;
        }

        public string[] EnemyPachimonInstanceIds { get; }

        public TrainerProfile TrainerProfile { get; }
    }

    public sealed class RestSpotNodeContent : NodeContent
    {
        public RestSpotNodeContent(int healPercent)
        {
            HealPercent = healPercent;
        }

        public int HealPercent { get; }
    }

    public sealed class CityNodeContent : NodeContent
    {
        public CityNodeContent(string cityGroupId, int shopSeed)
        {
            CityGroupId = cityGroupId;
            ShopSeed = shopSeed;
        }

        public string CityGroupId { get; }

        public int ShopSeed { get; }
    }

    public sealed class EventNodeContent : NodeContent
    {
        public EventNodeContent(int eventSeed)
        {
            EventSeed = eventSeed;
        }

        public int EventSeed { get; }
    }

    public enum LeagueGateFailureMode
    {
        SpecialDefeat = 0,
    }

    public sealed class LeagueGateNodeContent : NodeContent
    {
        public LeagueGateNodeContent(int requiredBadgeCount, LeagueGateFailureMode failureMode)
        {
            RequiredBadgeCount = requiredBadgeCount;
            FailureMode = failureMode;
        }

        public int RequiredBadgeCount { get; }

        public LeagueGateFailureMode FailureMode { get; }
    }
}
