using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class HealingWindSkillLogic : ISkillLogic
    {
        private readonly HealingWindSkillAsset _skill;
        public HealingWindSkillLogic(HealingWindSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Healing Wind Logic received another Skill.", nameof(context));
            if (_skill.StatusDefinition == null)
                throw new InvalidOperationException("Healing Wind requires a Status Definition.");

            var target = context.Targets.GetLowestHpPercentageAlly();
            var healing = Scale(context, _skill.BaseHealing);
            var windBonus = Scale(context, _skill.BaseWindBonus);
            var speedBonus = Scale(context, _skill.BaseSpeedBonus);
            context.State.SupportEffects.RestoreHp(
                context.User,
                target,
                healing);
            context.State.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.HealingWind,
                    BattleStatusCategory.None,
                    context.User,
                    value: 0,
                    durationTicks: _skill.DurationTicks,
                    runtimeData: new HealingWindRuntimeData(
                        windBonus,
                        speedBonus),
                    definition: _skill.StatusDefinition));

            return new SkillResolution(
                context.User,
                _skill,
                Array.Empty<SkillEffectResult>());
        }

        private int Scale(SkillExecutionContext context, int baseValue) =>
            SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    baseValue,
                    PachimonAttribute.Wind,
                    _skill.WindRatio));
    }
}
