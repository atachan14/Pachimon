using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class BackfireSkillLogic : ISkillLogic
    {
        private readonly BackfireSkillAsset _skill;

        public BackfireSkillLogic(BackfireSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            ValidateSkill(context);
            var target = context.Targets.GetBackEnemy()
                ?? throw new InvalidOperationException(
                    "No living Enemy target was found.");
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

            var baseDamage = BackfireMath.CalculateBaseDamage(
                _skill,
                user.GetBattleStatValue(PachimonStatType.Fire));
            var penetrationPercent = BackfireMath.CalculatePenetrationPercent(
                _skill,
                user.GetBattleStatValue(PachimonStatType.Poison));
            return AttributeDamageCalculator.Calculate(new DamageContext(
                DamageOriginKind.Skill,
                _skill.SkillId,
                baseDamage,
                user.GetBattleStats(),
                target.GetBattleStats(),
                PachimonAttribute.Fire,
                isAttack: true,
                applyAttackerAttributeMultiplier: false,
                penetrationPercent: penetrationPercent));
        }

        private void ValidateSkill(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Backfire Logic received another Skill Asset.",
                    nameof(context));
            }
        }
    }
}
