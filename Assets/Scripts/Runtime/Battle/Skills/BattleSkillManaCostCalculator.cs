using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public readonly struct BattleSkillManaSpendPlan
    {
        public BattleSkillManaSpendPlan(int actual, decimal effective)
        {
            Actual = actual;
            Effective = effective;
        }

        public int Actual { get; }
        public decimal Effective { get; }
    }

    public static class BattleSkillManaCostCalculator
    {
        public static BattleSkillManaSpendPlan CreatePlan(
            BattleUnitState user,
            SkillAsset skill)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (skill == null) throw new ArgumentNullException(nameof(skill));

            var multiplier = ResolveCostMultiplier(user);
            if (skill.ConsumesAllCurrentMana)
            {
                return new BattleSkillManaSpendPlan(
                    user.CurrentMn,
                    user.CurrentMn / multiplier);
            }

            var actual = SignedStatMath.CeilPositive(
                skill.BaseManaCost * multiplier);
            return new BattleSkillManaSpendPlan(actual, skill.BaseManaCost);
        }

        private static decimal ResolveCostMultiplier(BattleUnitState user)
        {
            var launch = user.GetStatus(BattleStatusId.LaunchCeremony);
            if (launch?.Definition is not LaunchCeremonyStatusAsset definition)
            {
                return 1m;
            }

            var scaledAqua = user.GetBattleStatValue(PachimonStatType.Aqua)
                * definition.ManaReductionAquaRatio / 100m;
            return SignedStatMath.ReductionMultiplier(scaledAqua);
        }
    }
}
