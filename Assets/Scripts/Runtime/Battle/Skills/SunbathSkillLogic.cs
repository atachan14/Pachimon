using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class SunbathSkillLogic : ISkillLogic
    {
        private readonly SunbathSkillAsset _skill;

        public SunbathSkillLogic(SunbathSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
            {
                throw new ArgumentException("Sunbath Logic received another Skill.", nameof(context));
            }

            var healing = SignedStatMath.ScaleFromBase(
                _skill.BaseHealing,
                context.User.GetBattleStatValue(PachimonStatType.Leaf),
                _skill.LeafHealingRatio);
            var temperature = Math.Max(0, context.State.Weather.Temperature);
            healing *= SignedStatMath.AmplificationMultiplier(
                temperature * _skill.TemperatureHealingRatio / 100m);
            var rain = context.State.Weather.IsRaining
                ? context.State.Weather.Get(BattleWeatherId.Rain)?.Value ?? 0
                : 0;
            if (rain > 0)
            {
                healing *= SignedStatMath.ReductionMultiplier(
                    rain * _skill.RainHealingReductionRatio / 100m);
            }

            var requested = SignedStatMath.FloorNonNegative(healing);
            context.State.SupportEffects.RestoreHp(
                context.User,
                context.User,
                requested);
            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
