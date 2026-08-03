using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class FireArrowSkillLogic : ISkillLogic
    {
        private readonly FireArrowSkillAsset _skill;

        public FireArrowSkillLogic(FireArrowSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            ValidateSkill(context);
            var effects = new List<SkillEffectResult>();
            var target = GetTarget(context);

            while (target != null)
            {
                var result = BattleAttributeDamageService.Apply(
                    context.State,
                    context.User,
                    target,
                    Calculate(context.User, target).Context);
                effects.Add(new SkillEffectResult(
                    target,
                    result.AppliedDamage,
                    isTrueDamage: false));

                if (!target.IsDefeated)
                {
                    break;
                }

                target = GetTarget(context);
                if (!context.User.IsAlive
                    || target == null
                    || !context.TrySpendAdditionalMn(_skill.BaseManaCost))
                {
                    break;
                }

                context.BeginNextPresentationBlock();
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                effects);
        }

        public DamageCalculationResult Calculate(
            BattleUnitState user,
            BattleUnitState target)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var baseDamage = FireArrowMath.CalculateBaseDamage(
                _skill,
                user.GetBattleStatValue(PachimonStatType.Fire));
            return AttributeDamageCalculator.Calculate(new DamageContext(
                DamageOriginKind.Skill,
                _skill.SkillId,
                baseDamage,
                user.GetBattleStats(),
                target.GetBattleStats(),
                PachimonAttribute.Fire,
                isAttack: true,
                applyAttackerAttributeMultiplier: false));
        }

        private static BattleUnitState GetTarget(
            SkillExecutionContext context)
        {
            return context.Targets.GetLowestHpEnemy();
        }

        private void ValidateSkill(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Fire Arrow Logic received another Skill Asset.",
                    nameof(context));
            }
        }
    }
}
