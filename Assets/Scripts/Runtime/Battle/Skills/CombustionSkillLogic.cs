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
                    CalculateEnemyDamage(context.State, context.User, target).Context);
                effects.Add(new SkillEffectResult(
                    enemyResult.ActualTarget,
                    enemyResult.AppliedDamage,
                    isTrueDamage: false));

                var selfResult = BattleAttributeDamageService.Apply(
                    context.State,
                    context.User,
                    context.User,
                    CalculateSelfDamage(context.State, context.User).Context);
                effects.Add(new SkillEffectResult(
                    selfResult.ActualTarget,
                    selfResult.AppliedDamage,
                    isTrueDamage: false));

                var affectedResources = enemyResult.AppliedDamage
                    + enemyResult.ShieldAbsorbedDamage
                    + selfResult.AppliedDamage
                    + selfResult.ShieldAbsorbedDamage;
                if (!context.User.IsAlive
                    || !target.IsAlive
                    || affectedResources == 0)
                {
                    break;
                }

                context.BeginNextPresentationBlock();
                context.State.AddLog(
                    $"{context.User.DisplayName}\u306f\u71c3\u713c\u3057\u3066\u3044\u308b\uff01");
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
            return CalculateEnemyDamage(state: null, user, target);
        }

        private DamageCalculationResult CalculateEnemyDamage(
            BattleState state,
            BattleUnitState user,
            BattleUnitState target)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var baseDamage = CombustionMath.CalculateBaseDamage(
                _skill,
                user.GetBattleStatValue(PachimonStatType.Fire),
                state?.ResolveAttributeRatio(
                    PachimonAttribute.Fire,
                    _skill.FireScalingPercent));
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
            return CalculateSelfDamage(state: null, user);
        }

        private DamageCalculationResult CalculateSelfDamage(
            BattleState state,
            BattleUnitState user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var baseDamage = CombustionMath.CalculateBaseDamage(
                _skill,
                user.GetBattleStatValue(PachimonStatType.Fire),
                state?.ResolveAttributeRatio(
                    PachimonAttribute.Fire,
                    _skill.FireScalingPercent));
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
