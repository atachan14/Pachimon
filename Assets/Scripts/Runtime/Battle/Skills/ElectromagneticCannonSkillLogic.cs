using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class ElectromagneticCannonSkillLogic : ISkillLogic
    {
        private readonly ElectromagneticCannonSkillAsset _skill;

        public ElectromagneticCannonSkillLogic(
            ElectromagneticCannonSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            ValidateSkill(context);
            var effects = new List<SkillEffectResult>();
            var transferredDamage = 0;
            var isInitialHit = true;
            foreach (var target in context.Targets.GetAllEnemies())
            {
                var calculation = isInitialHit
                    ? CalculateInitial(context.User, target)
                    : CalculateTransferred(
                        context.User,
                        target,
                        transferredDamage);
                var hpBeforeDamage = target.CurrentHp;
                var result = BattleAttributeDamageService.Apply(
                    context.State,
                    context.User,
                    target,
                    calculation.Context);
                effects.Add(new SkillEffectResult(
                    target,
                    result.AppliedDamage,
                    isTrueDamage: false));
                transferredDamage = CalculateOverflow(
                    result.FinalDamage,
                    hpBeforeDamage);
                if (transferredDamage <= 0)
                {
                    break;
                }

                isInitialHit = false;
            }

            return new SkillResolution(context.User, context.Skill, effects);
        }

        public DamageCalculationResult CalculateInitial(
            BattleUnitState user,
            BattleUnitState target)
        {
            ValidateUnits(user, target);
            return AttributeDamageCalculator.Calculate(new DamageContext(
                DamageOriginKind.Skill,
                _skill.SkillId,
                _skill.BasePower,
                user.GetBattleStats(),
                target.GetBattleStats(),
                PachimonAttribute.Electric,
                isAttack: true));
        }

        public DamageCalculationResult CalculateTransferred(
            BattleUnitState user,
            BattleUnitState target,
            int transferredDamage)
        {
            ValidateUnits(user, target);
            if (transferredDamage < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(transferredDamage));
            }

            return AttributeDamageCalculator.Calculate(new DamageContext(
                DamageOriginKind.Skill,
                _skill.SkillId,
                transferredDamage,
                user.GetBattleStats(),
                target.GetBattleStats(),
                PachimonAttribute.Electric,
                isAttack: true,
                applyAttackerAttributeMultiplier: false,
                penetrationPercent: 0m,
                applyDamageBonusMultiplier: false,
                applyOutgoingModifiers: false));
        }

        public static int CalculateOverflow(int damage, int currentHp)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (currentHp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            }

            return Math.Max(0, damage - currentHp);
        }

        private void ValidateSkill(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Electromagnetic Cannon Logic received another Skill Asset.",
                    nameof(context));
            }
        }

        private static void ValidateUnits(
            BattleUnitState user,
            BattleUnitState target)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (target == null) throw new ArgumentNullException(nameof(target));
        }
    }
}
