using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class ElectricExplosionSkillLogic : ISkillLogic
    {
        private readonly ElectricExplosionSkillAsset _skill;

        public ElectricExplosionSkillLogic(ElectricExplosionSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            ValidateSkill(context);
            var target = GetTarget(context);
            var result = BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                Calculate(context.User, target).Context);
            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        target,
                        result.AppliedDamage,
                        isTrueDamage: false),
                });
        }

        public DamageCalculationResult Calculate(
            BattleUnitState user,
            BattleUnitState target)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var electric = user.GetBattleStatValue(PachimonStatType.Electric);
            var fire = user.GetBattleStatValue(PachimonStatType.Fire);
            var baseDamage = ElectricExplosionMath.CalculateBaseDamage(
                _skill,
                electric,
                fire);
            var penetrationPercent =
                ElectricExplosionMath.CalculatePenetrationPercent(
                    _skill,
                    fire);
            return AttributeDamageCalculator.Calculate(new DamageContext(
                DamageOriginKind.Skill,
                _skill.SkillId,
                baseDamage,
                user.GetBattleStats(),
                target.GetBattleStats(),
                PachimonAttribute.Electric,
                isAttack: true,
                applyAttackerAttributeMultiplier: false,
                penetrationPercent: penetrationPercent));
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
                    "Electric Explosion Logic received another Skill Asset.",
                    nameof(context));
            }
        }
    }
}
