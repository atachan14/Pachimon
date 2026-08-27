using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class NeurotoxinMath
    {
        public static int CalculateStunTicks(
            NeurotoxinSkillAsset skill,
            int electric)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            var electricTicks = SignedStatMath.ScaleFromBase(
                skill.BaseElectricStunTicks,
                electric,
                skill.ElectricStunScalingPercent);
            return SignedStatMath.FloorNonNegative(electricTicks);
        }

        public static int CalculateToxinValue(
            NeurotoxinSkillAsset skill,
            int poison)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    skill.BaseToxinValue,
                    poison,
                    skill.ToxinScalingPercent));
        }
    }

    public sealed class NeurotoxinSkillLogic : ISkillLogic
    {
        private readonly NeurotoxinSkillAsset _skill;

        public NeurotoxinSkillLogic(NeurotoxinSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Neurotoxin Logic received another Skill Asset.",
                    nameof(context));
            }

            var target = context.Targets.GetBackEnemy()
                ?? throw new InvalidOperationException(
                    "No living Enemy target was found.");
            var hit = context.BeginStatusHit(target);
            var poison = context.User.GetBattleStatValue(PachimonStatType.Poison);
            var electric = context.User.GetBattleStatValue(
                PachimonStatType.Electric);
            var stunTicks = NeurotoxinMath.CalculateStunTicks(
                _skill,
                electric);
            var toxinValue = NeurotoxinMath.CalculateToxinValue(_skill, poison);

            if (stunTicks > 0)
            {
                hit.ApplyStatus(
                    BattleStatusFactory.CreateStun(
                        context.User,
                        stunTicks,
                        _skill.StunStatus));
            }

            if (toxinValue > 0)
            {
                hit.ApplyStatus(
                    BattleStatusFactory.CreateToxin(
                        context.User,
                        toxinValue,
                        _skill.ToxinStatus));
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
