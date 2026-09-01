using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class HealingWindSkillLogic : ISkillLogic
    {
        private readonly HealingWindSkillAsset _skill;
        public HealingWindSkillLogic(HealingWindSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Healing Wind Logic received another Skill.", nameof(context));
            var target = context.Targets.GetLowestHpPercentageAlly();
            var healing = Scale(context, _skill.BaseHealing);
            context.State.SupportEffects.RestoreHp(
                context.User,
                target,
                healing);

            return new SkillResolution(
                context.User,
                _skill,
                Array.Empty<SkillEffectResult>());
        }

        private int Scale(SkillExecutionContext context, int baseValue) =>
            SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    baseValue,
                    PachimonAttribute.Wind,
                    _skill.WindRatio));
    }
}
