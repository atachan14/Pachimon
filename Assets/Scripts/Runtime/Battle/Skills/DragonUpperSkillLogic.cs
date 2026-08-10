using System;
using Pachimon.Reward;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class DragonUpperSkillLogic : ISkillLogic
    {
        private readonly DragonUpperSkillAsset _skill;

        public DragonUpperSkillLogic(DragonUpperSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Dragon Upper Logic received another Skill.", nameof(context));
            if (_skill.KnockoutStatus == null)
                throw new InvalidOperationException("Dragon Upper requires Knockout Status.");

            var target = context.Targets.GetFrontEnemy();
            var damage = context.ScaleFromAttribute(
                _skill.BaseDragonDamage,
                PachimonAttribute.Dragon,
                _skill.DragonDamageRatio);
            var result = BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    _skill.SkillId,
                    damage,
                    context.User.GetBattleStats(),
                    target.GetBattleStats(),
                    PachimonAttribute.Dragon,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false));

            var actualTarget = result.ActualTarget;
            if (actualTarget.IsAlive && result.FinalDamage > 0)
            {
                context.State.Statuses.ApplyAttackStatus(
                    actualTarget,
                    new BattleStatusInstance(
                        BattleStatusId.Knockout,
                        BattleStatusCategory.Stun,
                        context.User,
                        value: 0,
                        durationTicks: _skill.KnockoutDurationTicks,
                        definition: _skill.KnockoutStatus));
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        actualTarget,
                        result.AppliedDamage,
                        false),
                });
        }
    }
}
