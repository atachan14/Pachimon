using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class FlyingAttackSkillLogic : IStartupSkillLogic
    {
        private readonly FlyingAttackSkillAsset _skill;

        public FlyingAttackSkillLogic(FlyingAttackSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public object BeginStartup(SkillExecutionContext context)
        {
            Validate(context);
            var flying = new BattleStatusInstance(
                BattleStatusId.Flying,
                BattleStatusCategory.Untargetable,
                context.User,
                value: 0,
                definition: _skill.FlyingStatus);
            context.State.Statuses.ApplyStatus(context.User, flying);
            return flying;
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            Validate(context);
            if (context.RuntimeData is BattleStatusInstance flying)
            {
                context.User.TryRemoveStatusInstance(flying);
            }

            var target = context.Targets.GetFrontEnemy();
            var damage = context.ScaleFromAttribute(
                _skill.BaseWindDamage,
                PachimonAttribute.Wind,
                _skill.WindDamageRatio);
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
                    PachimonAttribute.Wind,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false));
            return new SkillResolution(
                context.User,
                _skill,
                new[] { new SkillEffectResult(result.ActualTarget, result.AppliedDamage, false) });
        }

        private void Validate(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Flying Attack Logic received another Skill.", nameof(context));
            if (_skill.FlyingStatus == null)
                throw new InvalidOperationException("Flying Attack requires a Flying Status.");
        }
    }
}
