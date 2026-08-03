using System;

namespace Pachimon.Battle
{
    public sealed class BattleSkillCooldownState
    {
        public BattleSkillCooldownState(
            decimal totalWork,
            decimal remainingWork)
        {
            if (totalWork < 0m) throw new ArgumentOutOfRangeException(nameof(totalWork));
            if (remainingWork < 0m || remainingWork > totalWork)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingWork));
            }

            TotalWork = totalWork;
            RemainingWork = remainingWork;
        }

        public decimal TotalWork { get; }
        public decimal RemainingWork { get; private set; }
        public bool IsReady => RemainingWork <= 0m;

        internal void Advance(decimal progress)
        {
            if (progress < 0m) throw new ArgumentOutOfRangeException(nameof(progress));
            RemainingWork = Math.Max(0m, RemainingWork - progress);
        }

        internal BattleSkillCooldownState CreateSimulationClone()
        {
            return new BattleSkillCooldownState(TotalWork, RemainingWork);
        }
    }
}
