using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class ElectricQuickAttackSkillLogic : ISkillLogic
    {
        private readonly ElectricQuickAttackSkillAsset _skill;

        public ElectricQuickAttackSkillLogic(
            ElectricQuickAttackSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            ValidateSkill(context);
            var target = GetTarget(context);
            var hit = context.BeginAttackHit(target);
            var result = ResolveDamage(context, target, hit);
            var effects = new[]
                {
                    new SkillEffectResult(
                        hit.Target,
                        result.AppliedDamage,
                        isTrueDamage: false,
                        hit: hit),
                };
            return new SkillResolution(
                context.User,
                context.Skill,
                effects);
        }

        public DamageCalculationResult CalculateDamage(
            BattleUnitState user,
            BattleUnitState target)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var electric = user.GetBattleStatValue(PachimonStatType.Electric);
            var baseDamage = ElectricQuickAttackMath.CalculateElectricBaseDamage(
                _skill,
                electric);
            return AttributeDamageCalculator.Calculate(new DamageContext(
                DamageOriginKind.Skill,
                _skill.SkillId,
                baseDamage,
                user.GetBattleStats(),
                target.GetBattleStats(),
                PachimonAttribute.Electric,
                isAttack: true,
                applyAttackerAttributeMultiplier: false));
        }

        private BattleDamageApplicationResult ResolveDamage(
            SkillExecutionContext context,
            BattleUnitState target,
            SkillHit hit)
        {
            return BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                CalculateDamage(context.User, target).Context,
                hit);
        }

        private static BattleUnitState GetTarget(SkillExecutionContext context)
        {
            return context.Targets.GetFrontEnemy()
                ?? throw new InvalidOperationException(
                    "No living Enemy target was found.");
        }

        private void ValidateSkill(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Electric Quick Attack Logic received another Skill Asset.",
                    nameof(context));
            }
        }
    }
}
