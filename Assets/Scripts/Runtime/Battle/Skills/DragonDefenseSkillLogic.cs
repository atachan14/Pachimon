using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class DragonDefenseSkillLogic : ISkillLogic
    {
        private readonly DragonDefenseSkillAsset _skill;

        public DragonDefenseSkillLogic(DragonDefenseSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Dragon Defense Logic received another Skill.", nameof(context));
            if (_skill.DefenseStatus == null)
                throw new InvalidOperationException("Dragon Defense requires Status Definition.");

            var shieldValue = SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    _skill.BaseShieldValue,
                    PachimonAttribute.Dragon,
                    _skill.DragonShieldRatio));
            if (shieldValue > 0)
            {
                context.State.SupportEffects.ApplyShield(
                    context.User,
                    context.User,
                    shieldValue,
                    _skill.DurationTicks);
            }
            context.State.Statuses.ApplyStatus(
                context.User,
                new BattleStatusInstance(
                    BattleStatusId.DragonDefense,
                    BattleStatusCategory.None,
                    context.User,
                    value: 0,
                    durationTicks: _skill.DurationTicks,
                    definition: _skill.DefenseStatus));
            context.State.Presentation.RecordLog(
                $"{context.User.DisplayName}は味方を守る構えを取った！");

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
