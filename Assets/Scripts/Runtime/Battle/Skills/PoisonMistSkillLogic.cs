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

            var value = _skill.CalculateMistValue(
                context.GetAttributeValue(PachimonAttribute.Poison));
            var duration = _skill.CalculateDurationTicks(
                context.GetAttributeValue(PachimonAttribute.Aqua));
            var minimumValue = _skill.CalculateMinimumValue(
                context.GetAttributeValue(PachimonAttribute.Poison),
                context.GetAttributeValue(PachimonAttribute.Wind));
            context.State.Fields.CreatePoisonMist(
                context.User,
                _skill.FieldEffect,
                value,
                duration,
                minimumValue);
            return new SkillResolution(
                context.User,
                _skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
