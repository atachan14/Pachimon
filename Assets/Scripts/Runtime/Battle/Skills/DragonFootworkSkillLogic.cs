using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class DragonFootworkSkillLogic : ISkillLogic
    {
        private readonly DragonFootworkSkillAsset _skill;

        public DragonFootworkSkillLogic(DragonFootworkSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Dragon Footwork Logic received another Skill.", nameof(context));
            if (_skill.FootworkStatus == null)
                throw new InvalidOperationException("Dragon Footwork requires a Footwork Status.");

            var durationTicks = Math.Max(
                1,
                SignedStatMath.FloorNonNegative(
                    context.ScaleFromAttribute(
                        _skill.BaseDurationTicks,
                        PachimonAttribute.Dragon,
                        _skill.DurationDragonRatio)));
            context.State.Statuses.ApplyStatus(
                context.User,
                new BattleStatusInstance(
                    BattleStatusId.Footwork,
                    BattleStatusCategory.None,
                    context.User,
                    value: 0,
                    durationTicks: durationTicks,
                    definition: _skill.FootworkStatus));
            context.State.Presentation.RecordLog(
                $"{context.User.DisplayName}はフットワークを始めた！");

            return new SkillResolution(
                context.User,
                _skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
