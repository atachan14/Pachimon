using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class CombustionSkillLogic : ISkillLogic
    {
        private readonly CombustionSkillAsset _skill;

        public CombustionSkillLogic(CombustionSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            ValidateSkill(context);
            var target = context.Targets.GetFrontEnemy()
                ?? throw new InvalidOperationException(
                    "No living Enemy target was found.");
            var effects = new List<SkillEffectResult>();

            while (true)
            {
                var enemyResult = BattleAttributeDamageService.Apply(
                    context.State,
                    context.User,
                    target,
                    CalculateEnemyDamage(context.User, target).Context);
                effects.Add(new SkillEffectResult(
                    target,
                    enemyResult.AppliedDamage,
                    isTrueDamage: false));

                var selfResult = BattleAttributeDamageService.Apply(
                    context.State,
                    context.User,
                    context.User,
                    CalculateSelfDamage(context.User).Context);
                effects.Add(new SkillEffectResult(
                    context.User,
                    selfResult.AppliedDamage,
                    isTrueDamage: false));

                if (!context.User.IsAlive
                    || !target.IsAlive
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

        public DamageCalculationResult CalculateEnemyDamage(
            BattleUnitState user,
            BattleUnitState target)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var baseDamage = CombustionMath.CalculateBaseDamage(
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

        public DamageCalculationResult CalculateSelfDamage(
            BattleUnitState user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var baseDamage = CombustionMath.CalculateBaseDamage(
                _skill,
                user.GetBattleStatValue(PachimonStatType.Fire));
            return AttributeDamageCalculator.Calculate(new DamageContext(
                DamageOriginKind.Skill,
                _skill.SkillId,
                baseDamage,
                user.GetBattleStats(),
                user.GetBattleStats(),
                PachimonAttribute.Fire,
                isAttack: true,
                applyAttackerAttributeMultiplier: false));
        }

        private void ValidateSkill(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Combustion Logic received another Skill Asset.",
                    nameof(context));
            }
        }
    }
}
