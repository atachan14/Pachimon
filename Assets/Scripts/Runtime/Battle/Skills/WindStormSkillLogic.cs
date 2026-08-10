using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class WindStormSkillLogic : ISkillLogic
    {
        private readonly WindStormSkillAsset _skill;

        public WindStormSkillLogic(WindStormSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Wind Storm Logic received another Skill Asset.",
                    nameof(context));
            }
            if (_skill.WindDefinition == null)
            {
                throw new InvalidOperationException(
                    "Wind Definition is not assigned.");
            }

            var value = SignedStatMath.FloorNonNegative(
                _skill.BaseValue
                + context.GetAttributeValue(PachimonAttribute.Wind)
                * context.GetAttributeRatio(
                    PachimonAttribute.Wind,
                    _skill.WindValueRatio) / 100m,
                minimum: 1);
            var weather = context.State.Weather.CreateOrAdd(
                context.User,
                _skill.WindDefinition,
                value);
            context.State.AddLog(
                $"\u66B4\u98A8\u306EValue\u304C{value}\u5897\u52A0\u3057\u305F\uFF01"
                + $"\uFF08\u73FE\u5728\u306EValue: {weather.Value}\uFF09");

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
