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
            var electricResult = ResolveComponent(
                context,
                target,
                PachimonAttribute.Electric);
            var fireResult = ResolveComponent(
                context,
                target,
                PachimonAttribute.Fire);
            var effects = ReferenceEquals(
                    electricResult.ActualTarget,
                    fireResult.ActualTarget)
                ? new[]
                {
                    new SkillEffectResult(
                        fireResult.ActualTarget,
                        checked(electricResult.AppliedDamage + fireResult.AppliedDamage),
                        isTrueDamage: false),
                }
                : new[]
                {
                    new SkillEffectResult(
                        electricResult.ActualTarget,
                        electricResult.AppliedDamage,
                        isTrueDamage: false),
                    new SkillEffectResult(
                        fireResult.ActualTarget,
                        fireResult.AppliedDamage,
                        isTrueDamage: false),
                };
            return new SkillResolution(
                context.User,
                context.Skill,
                effects);
        }

        public DamageCalculationResult CalculateComponent(
            BattleUnitState user,
            BattleUnitState target,
            PachimonAttribute attribute)
        {
            return CalculateComponent(state: null, user, target, attribute);
        }

        private DamageCalculationResult CalculateComponent(
            BattleState state,
            BattleUnitState user,
            BattleUnitState target,
            PachimonAttribute attribute)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var statType = PachimonStatTypeUtility.FromAttribute(attribute);
            var attributeValue = user.GetBattleStatValue(statType);
            var baseDamage = attribute switch
            {
                PachimonAttribute.Electric =>
                    ElectricQuickAttackMath.CalculateElectricBaseDamage(
                        _skill,
                        attributeValue),
                PachimonAttribute.Fire =>
                    ElectricQuickAttackMath.CalculateFireBaseDamage(
                        _skill,
                        attributeValue,
                        state?.ResolveAttributeRatio(
                            PachimonAttribute.Fire,
                            100m) ?? 100m),
                _ => throw new ArgumentOutOfRangeException(nameof(attribute)),
            };
            return AttributeDamageCalculator.Calculate(new DamageContext(
                DamageOriginKind.Skill,
                _skill.SkillId,
                baseDamage,
                user.GetBattleStats(),
                target.GetBattleStats(),
                attribute,
                isAttack: true,
                applyAttackerAttributeMultiplier: false));
        }

        private BattleDamageApplicationResult ResolveComponent(
            SkillExecutionContext context,
            BattleUnitState target,
            PachimonAttribute attribute)
        {
            var result = BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                CalculateComponent(
                    context.State,
                    context.User,
                    target,
                    attribute).Context);
            return result;
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
