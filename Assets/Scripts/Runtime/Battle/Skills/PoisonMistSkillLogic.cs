using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class PoisonMistSkillLogic : ISkillLogic
    {
        private readonly PoisonMistSkillAsset _skill;

        public PoisonMistSkillLogic(PoisonMistSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Poison Mist received another Skill.");

            var value = SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    _skill.BaseMistValue,
                    PachimonAttribute.Poison,
                    _skill.PoisonValueRatio));
            var duration = Math.Max(1, SignedStatMath.FloorNonNegative(
                context.GetAttributeValue(PachimonAttribute.Aqua)
                    * _skill.AquaDurationRatio / 100m
                + context.GetAttributeValue(PachimonAttribute.Wind)
                    * _skill.WindDurationRatio / 100m));
            context.State.Fields.CreatePoisonMist(
                context.User,
                _skill.FieldEffect,
                Math.Max(1, value),
                duration);
            return new SkillResolution(
                context.User,
                _skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
