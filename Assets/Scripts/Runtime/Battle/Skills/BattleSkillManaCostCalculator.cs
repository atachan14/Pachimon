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
            if (skill is WaterPulseReplacementSkillAsset regularWaterPulse)
            {
                var baseCost = Math.Max(
                    1,
                    SignedStatMath.FloorNonNegative(
                        user.MaxMn * regularWaterPulse.MaxMnCostPercent / 100m));
                return new BattleSkillManaSpendPlan(
                    SignedStatMath.CeilPositive(baseCost * multiplier),
                    baseCost);
            }
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

        public static BattleSkillManaSpendPlan CreatePlan(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var defaultPlan = CreatePlan(user, skill);
            if (skill is not WaterPulseSkillAsset waterPulse
                || user.CurrentMn <= 0)
            {
                return defaultPlan;
            }

            var multiplier = ResolveCostMultiplier(user);
            var requiredActualMana = FindWaterPulseManaToDefeat(
                state,
                user,
                waterPulse,
                multiplier);
            return requiredActualMana.HasValue
                ? new BattleSkillManaSpendPlan(
                    requiredActualMana.Value,
                    requiredActualMana.Value / multiplier)
                : defaultPlan;
        }

        private static int? FindWaterPulseManaToDefeat(
            BattleState state,
            BattleUnitState user,
            WaterPulseSkillAsset skill,
            decimal costMultiplier)
        {
            if (!WaterPulseDefeatsTarget(
                    state,
                    user,
                    skill,
                    user.CurrentMn,
                    costMultiplier))
            {
                return null;
            }

            var lower = 1;
            var upper = user.CurrentMn;
            while (lower < upper)
            {
                var middle = lower + (upper - lower) / 2;
                if (WaterPulseDefeatsTarget(
                        state,
                        user,
                        skill,
                        middle,
                        costMultiplier))
                {
                    upper = middle;
                }
                else
                {
                    lower = middle + 1;
                }
            }

            return lower;
        }

        private static bool WaterPulseDefeatsTarget(
            BattleState state,
            BattleUnitState user,
            WaterPulseSkillAsset skill,
            int actualMana,
            decimal costMultiplier)
        {
            var snapshot = BattleSimulationSnapshot.Create(state);
            var simulationUser = snapshot.GetSimulationUnit(user);
            BattleUnitState target;
            try
            {
                target = new BattleTargetQuery(
                    snapshot.State,
                    simulationUser).GetFrontEnemy();
            }
            catch (SkillTargetUnavailableException)
            {
                return false;
            }

            var hpBefore = target.CurrentHp;
            var resolution = new WaterPulseSkillLogic(skill).Resolve(
                new SkillExecutionContext(
                    snapshot.State,
                    simulationUser,
                    skill,
                    actualManaSpent: actualMana,
                    effectiveManaSpent: actualMana / costMultiplier));
            return resolution.Effects.Count > 0
                && ReferenceEquals(resolution.Effects[0].Target, target)
                && resolution.Effects[0].Damage >= hpBefore;
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
