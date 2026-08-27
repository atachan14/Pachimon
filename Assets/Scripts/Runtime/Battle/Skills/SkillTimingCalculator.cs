using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public readonly struct SkillTimingMultipliers
    {
        public SkillTimingMultipliers(
            decimal startup,
            decimal recovery,
            decimal cooldown)
        {
            if (startup <= 0m) throw new ArgumentOutOfRangeException(nameof(startup));
            if (recovery <= 0m) throw new ArgumentOutOfRangeException(nameof(recovery));
            if (cooldown <= 0m) throw new ArgumentOutOfRangeException(nameof(cooldown));

            Startup = startup;
            Recovery = recovery;
            Cooldown = cooldown;
        }

        public decimal Startup { get; }
        public decimal Recovery { get; }
        public decimal Cooldown { get; }

        public static SkillTimingMultipliers Neutral =>
            new(1m, 1m, 1m);
    }

    public readonly struct BattleSkillTimingPlan
    {
        public BattleSkillTimingPlan(
            decimal startupTicks,
            decimal recoveryTicks,
            decimal cooldownTicks)
        {
            if (startupTicks < 0m) throw new ArgumentOutOfRangeException(nameof(startupTicks));
            if (recoveryTicks < 0m) throw new ArgumentOutOfRangeException(nameof(recoveryTicks));
            if (cooldownTicks < 0m) throw new ArgumentOutOfRangeException(nameof(cooldownTicks));

            StartupWork = startupTicks;
            RecoveryWork = recoveryTicks;
            CooldownWork = cooldownTicks;
        }

        public decimal StartupWork { get; }
        public decimal RecoveryWork { get; }
        public decimal CooldownWork { get; }
        public int StartupTicks => SignedStatMath.CeilPositive(StartupWork);
        public int RecoveryTicks => SignedStatMath.CeilPositive(RecoveryWork);
        public int CooldownTicks => SignedStatMath.CeilPositive(CooldownWork);
    }

    public static class SkillTimingCalculator
    {
        public static BattleSkillTimingPlan CreatePlan(
            SkillAsset skill,
            EffectivePachimonStats stats,
            int upgradeLevel = 0)
        {
            var multipliers = Calculate(skill, stats);
            var upgradeMultiplier = SkillUpgradeMath.GetTimingMultiplier(upgradeLevel);
            return new BattleSkillTimingPlan(
                skill.BaseStartupTicks * multipliers.Startup * upgradeMultiplier,
                skill.BaseRecoveryTicks * multipliers.Recovery * upgradeMultiplier,
                skill.BaseCooldownTicks * multipliers.Cooldown);
        }

        public static BattleSkillTimingPlan CreatePlan(
            SkillAsset skill,
            BattleUnitState unit,
            int upgradeLevel = 0)
        {
            return CreatePlan(skill, unit, state: null, upgradeLevel);
        }

        public static BattleSkillTimingPlan CreatePlan(
            SkillAsset skill,
            BattleUnitState unit,
            BattleState state,
            int upgradeLevel = 0)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (unit == null) throw new ArgumentNullException(nameof(unit));

            var multipliers = Calculate(
                skill,
                unit.GetBattleStatValue(PachimonStatType.Fire));
            var baseRecoveryTicks = skill is FrozenBreakSkillAsset frozenBreak
                && unit.GetStatus(BattleStatusId.FrozenBreakSelf) != null
                    ? frozenBreak.LowHpRecoveryTicks
                    : skill.BaseRecoveryTicks;
            var startupMultiplier = multipliers.Startup;
            var recoveryMultiplier = multipliers.Recovery;
            var oneTwo = unit.GetStatus(BattleStatusId.OneTwo);
            if (oneTwo?.Value > 0)
            {
                var oneTwoMultiplier = SignedStatMath.ReductionMultiplier(
                    oneTwo.Value);
                startupMultiplier *= oneTwoMultiplier;
                recoveryMultiplier *= oneTwoMultiplier;
            }
            if (skill is SolarBeamSkillAsset solarBeam
                && state?.Weather.Temperature > 0)
            {
                startupMultiplier *= SignedStatMath.ReductionMultiplier(
                    state.Weather.Temperature
                    * solarBeam.TemperatureStartupRatio / 100m);
            }
            var upgradeMultiplier = SkillUpgradeMath.GetTimingMultiplier(upgradeLevel);
            return new BattleSkillTimingPlan(
                skill.BaseStartupTicks * startupMultiplier * upgradeMultiplier,
                baseRecoveryTicks * recoveryMultiplier * upgradeMultiplier,
                skill.BaseCooldownTicks * multipliers.Cooldown);
        }

        public static SkillTimingMultipliers Calculate(
            SkillAsset skill,
            EffectivePachimonStats stats)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (stats == null) throw new ArgumentNullException(nameof(stats));

            if (skill is not ElectricQuickAttackSkillAsset quickAttack)
            {
                return SkillTimingMultipliers.Neutral;
            }

            return Calculate(
                skill,
                stats.GetValue(PachimonStatType.Fire));
        }

        private static SkillTimingMultipliers Calculate(
            SkillAsset skill,
            decimal fire)
        {
            if (skill is not ElectricQuickAttackSkillAsset quickAttack)
            {
                return SkillTimingMultipliers.Neutral;
            }

            var fireTimingMultiplier = CalculateFireTimingMultiplier(
                quickAttack,
                fire);
            return new SkillTimingMultipliers(
                startup: fireTimingMultiplier,
                recovery: fireTimingMultiplier,
                cooldown: 1m);
        }

        public static decimal CalculateFireTimingMultiplier(
            ElectricQuickAttackSkillAsset skill,
            decimal fire)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            var scaledFire = fire * skill.FireTimingPercent / 100m;
            return SignedStatMath.ReductionMultiplier(scaledFire);
        }
    }
}
