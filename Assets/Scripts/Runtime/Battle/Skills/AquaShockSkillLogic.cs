using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class AquaShockSkillLogic : ISkillLogic
    {
        private readonly AquaShockSkillAsset _skill;

        public AquaShockSkillLogic(AquaShockSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            ValidateSkill(context);
            var target = GetTarget(context);
            var electricDamage = ResolveComponent(
                context,
                target,
                PachimonAttribute.Electric);
            var aquaDamage = ResolveComponent(
                context,
                target,
                PachimonAttribute.Aqua);

            if (target.IsAlive)
            {
                var aqua = context.User.GetBattleStatValue(
                    PachimonStatType.Aqua);
                target.ApplyOrReplaceStatus(new BattleStatusInstance(
                    BattleStatusId.Leak,
                    BattleStatusCategory.Leak,
                    context.User,
                    AquaShockMath.CalculateLeakValue(_skill, aqua)));
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        target,
                        checked(electricDamage + aquaDamage),
                        isTrueDamage: false),
                });
        }

        public DamageContext CreateDamageContext(
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
                    AquaShockMath.CalculateElectricBaseDamage(
                        _skill,
                        attributeValue),
                PachimonAttribute.Aqua =>
                    AquaShockMath.CalculateAquaBaseDamage(
                        _skill,
                        attributeValue),
                _ => throw new ArgumentOutOfRangeException(nameof(attribute)),
            };
            return new DamageContext(
                DamageOriginKind.Skill,
                _skill.SkillId,
                baseDamage,
                user.GetBattleStats(),
                target.GetBattleStats(),
                attribute,
                isAttack: true,
                applyAttackerAttributeMultiplier: false);
        }

        private int ResolveComponent(
            SkillExecutionContext context,
            BattleUnitState target,
            PachimonAttribute attribute)
        {
            return BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                CreateDamageContext(context.User, target, attribute))
                .AppliedDamage;
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
                    "Aqua Shock Logic received another Skill Asset.",
                    nameof(context));
            }
        }
    }
}
