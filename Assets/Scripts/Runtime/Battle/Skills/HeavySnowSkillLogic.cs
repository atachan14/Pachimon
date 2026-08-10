using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class HeavySnowSkillLogic : ISkillLogic
    {
        private readonly HeavySnowSkillAsset _skill;

        public HeavySnowSkillLogic(HeavySnowSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Heavy Snow Logic received another Skill Asset.",
                    nameof(context));
            }
            if (_skill.TemperatureDefinition == null)
            {
                throw new InvalidOperationException(
                    "Temperature Definition is not assigned.");
            }

            var value = SignedStatMath.FloorNonNegative(
                _skill.BaseValue
                + context.GetAttributeValue(PachimonAttribute.Ice)
                * context.GetAttributeRatio(
                    PachimonAttribute.Ice,
                    _skill.IceValueRatio) / 100m,
                minimum: 1);
            if (value > 0)
            {
                var temperature = context.State.Weather.AddTemperature(
                    context.User,
                    _skill.TemperatureDefinition,
                    -value);
                context.State.AddLog(
                    $"気温が{value}低下した！ 現在の気温: {temperature:+#;-#;0}");
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
