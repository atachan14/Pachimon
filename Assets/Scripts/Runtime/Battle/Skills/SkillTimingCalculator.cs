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
            EffectivePachimonStats stats)
        {
            var multipliers = Calculate(skill, stats);
            return new BattleSkillTimingPlan(
                skill.BaseStartupTicks * multipliers.Startup,
                skill.BaseRecoveryTicks * multipliers.Recovery,
                skill.BaseCooldownTicks * multipliers.Cooldown);
        }

        public static BattleSkillTimingPlan CreatePlan(
            SkillAsset skill,
            BattleUnitState unit)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (unit == null) throw new ArgumentNullException(nameof(unit));

            var multipliers = Calculate(
                skill,
                unit.GetBattleStatValue(PachimonStatType.Wind));
            return new BattleSkillTimingPlan(
                skill.BaseStartupTicks * multipliers.Startup,
                skill.BaseRecoveryTicks * multipliers.Recovery,
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
                stats.GetValue(PachimonStatType.Wind));
        }

        private static SkillTimingMultipliers Calculate(
            SkillAsset skill,
            decimal wind)
        {
            if (skill is not ElectricQuickAttackSkillAsset quickAttack)
            {
                return SkillTimingMultipliers.Neutral;
            }

            var windMultiplier = CalculateWindMultiplier(quickAttack, wind);
            return new SkillTimingMultipliers(
                startup: 1m,
                recovery: windMultiplier,
                cooldown: windMultiplier);
        }

        public static decimal CalculateWindMultiplier(
            ElectricQuickAttackSkillAsset skill,
            decimal wind)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            var scaledWind = wind * skill.WindTimingPercent / 100m;
            return SignedStatMath.ReductionMultiplier(scaledWind);
        }
    }
}
