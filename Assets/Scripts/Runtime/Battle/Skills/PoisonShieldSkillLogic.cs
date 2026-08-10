using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class PoisonShieldMath
    {
        public static int CalculateShieldValue(
            PoisonShieldSkillAsset skill,
            int poison)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    skill.BaseShieldValue,
                    poison,
                    skill.ShieldPoisonScalingPercent));
        }

        public static decimal CalculateToxinReductionPercent(
            PoisonShieldSkillAsset skill,
            int poison)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.ScaleFromBase(
                skill.BaseToxinReductionPercent,
                poison,
                skill.ReductionPoisonScalingPercent);
        }

        public static int CalculateToxinReduction(
            PoisonShieldSkillAsset skill,
            int poison,
            int toxinValue)
        {
            if (toxinValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(toxinValue));
            }

            return SignedStatMath.FloorNonNegative(
                toxinValue
                * CalculateToxinReductionPercent(skill, poison)
                / 100m);
        }
    }

    public sealed class PoisonShieldSkillLogic : ISkillLogic
    {
        private readonly PoisonShieldSkillAsset _skill;

        public PoisonShieldSkillLogic(PoisonShieldSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Poison Shield Logic received another Skill Asset.",
                    nameof(context));
            }

            var poison = context.User.GetBattleStatValue(PachimonStatType.Poison);
            var shieldValue = PoisonShieldMath.CalculateShieldValue(
                _skill,
                poison);
            if (shieldValue > 0)
            {
                context.State.SupportEffects.ApplyShield(
                    context.User,
                    context.User,
                    shieldValue);
                context.State.AddLog(
                    $"{context.User.DisplayName}は{shieldValue}のShieldを得た！");
            }

            var toxin = context.User.GetStatus(BattleStatusId.Toxin);
            var requestedReduction = PoisonShieldMath.CalculateToxinReduction(
                _skill,
                poison,
                toxin?.Value ?? 0);
            var reducedToxin = context.State.Statuses.ReduceStatusValue(
                context.User,
                BattleStatusId.Toxin,
                requestedReduction);
            if (reducedToxin > 0)
            {
                context.State.AddLog(
                    $"{context.User.DisplayName}の毒素を{reducedToxin}取り除いた！");
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
