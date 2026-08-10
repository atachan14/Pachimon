using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class SunnyDaySkillLogic : ISkillLogic
    {
        private readonly SunnyDaySkillAsset _skill;

        public SunnyDaySkillLogic(SunnyDaySkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Sunny Day Logic received another Skill Asset.",
                    nameof(context));
            }
            if (_skill.TemperatureDefinition == null)
            {
                throw new InvalidOperationException(
                    "Temperature Definition is not assigned.");
            }

            var value = SignedStatMath.FloorNonNegative(
                _skill.BaseValue
                + context.GetAttributeValue(PachimonAttribute.Fire)
                * context.GetAttributeRatio(
                    PachimonAttribute.Fire,
                    _skill.FireValueRatio) / 100m,
                minimum: 1);
            if (value > 0)
            {
                var temperature = context.State.Weather.AddTemperature(
                    context.User,
                    _skill.TemperatureDefinition,
                    value);
                context.State.AddLog(
                    $"気温が{value}上昇した！ 現在の気温: {temperature:+#;-#;0}");
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
