using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class LightningCloudSkillLogic : ISkillLogic
    {
        private readonly LightningCloudSkillAsset _skill;
        public LightningCloudSkillLogic(LightningCloudSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var value = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _skill.BaseValue,
                    context.GetAttributeValue(PachimonAttribute.Electric),
                    context.GetAttributeRatio(
                        PachimonAttribute.Electric,
                        _skill.ElectricValueRatio)),
                minimum: 1);
            var weather = context.State.Weather.CreateOrAdd(
                context.User,
                _skill.ThunderDefinition,
                value);
            context.State.AddLog(
                $"雷のValueが{value}増加した！（現在のValue: {weather.Value}）");
            return new SkillResolution(context.User, _skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
