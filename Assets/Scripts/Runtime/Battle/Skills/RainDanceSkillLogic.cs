using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class RainDanceSkillLogic : ISkillLogic
    {
        private readonly RainDanceSkillAsset _skill;

        public RainDanceSkillLogic(RainDanceSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Rain Dance Logic received another Skill Asset.",
                    nameof(context));
            }
            if (_skill.RainDefinition == null)
            {
                throw new InvalidOperationException(
                    "Rain Definition is not assigned.");
            }

            var value = SignedStatMath.FloorNonNegative(
                _skill.BaseValue
                + context.GetAttributeValue(PachimonAttribute.Aqua)
                * context.GetAttributeRatio(
                    PachimonAttribute.Aqua,
                    _skill.AquaValueRatio) / 100m,
                minimum: 1);
            var rain = context.State.Weather.CreateOrAdd(
                context.User,
                _skill.RainDefinition,
                value);
            context.State.AddLog(
                $"雨が{value}強くなった！ 現在の雨: {rain.Value}");

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
