using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class SecondWindSkillLogic : ISkillLogic
    {
        private readonly SecondWindSkillAsset _skill;
        public SecondWindSkillLogic(SecondWindSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Second Wind Logic received another Skill.", nameof(context));
            if (_skill.StillAirStatus == null)
                throw new InvalidOperationException("Second Wind requires a Still Air Status.");

            var wind = context.User.GetBattleStatValue(PachimonStatType.Wind);
            var shield = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _skill.BaseShieldValue,
                    wind,
                    _skill.WindShieldRatio));
            if (shield > 0)
            {
                context.State.SupportEffects.ApplyShield(
                    context.User,
                    context.User,
                    shield,
                    _skill.DurationTicks);
            }
            context.State.Statuses.ApplyStatus(
                context.User,
                new BattleStatusInstance(
                    BattleStatusId.StillAir,
                    BattleStatusCategory.None,
                    context.User,
                    value: 0,
                    durationTicks: _skill.DurationTicks,
                    definition: _skill.StillAirStatus));

            return new SkillResolution(
                context.User,
                _skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
