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
            if (_skill.PrecipitationDefinition == null)
            {
                throw new InvalidOperationException(
                    "Precipitation Definition is not assigned.");
            }

            var value = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _skill.BaseValue,
                    context.GetAttributeValue(PachimonAttribute.Fire),
                    context.GetAttributeRatio(
                        PachimonAttribute.Fire,
                        _skill.FireValueRatio)),
                minimum: 1);
            if (value > 0)
            {
                var precipitation = context.State.Weather.AddPrecipitation(
                    context.User,
                    _skill.PrecipitationDefinition,
                    -value);
                context.State.AddLog(
                    $"晴天が{value}強くなった！ 現在の降水: {precipitation:+#;-#;0}");
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
