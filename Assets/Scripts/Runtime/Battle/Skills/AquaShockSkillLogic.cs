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
            var hit = context.BeginAttackHit(target);
            var electricResult = ResolveComponent(
                context,
                target,
                PachimonAttribute.Electric,
                hit);
            var aquaResult = ResolveComponent(
                context,
                target,
                PachimonAttribute.Aqua,
                hit);

            var statusTarget = aquaResult.ActualTarget;
            if (statusTarget.IsAlive)
            {
                var aqua = context.User.GetBattleStatValue(PachimonStatType.Aqua);
                hit.ApplyStatus(
                    new BattleStatusInstance(
                        BattleStatusId.Leak,
                        BattleStatusCategory.Leak,
                        context.User,
                        AquaShockMath.CalculateLeakValue(
                            _skill,
                            aqua,
                            context.GetAttributeRatio(PachimonAttribute.Aqua))));
            }

            var effects = new[]
                {
                    new SkillEffectResult(
                        hit.Target,
                        checked(electricResult.AppliedDamage + aquaResult.AppliedDamage),
                        isTrueDamage: false,
                        hit: hit),
                };
            return new SkillResolution(
                context.User,
                context.Skill,
                effects);
        }

        public DamageContext CreateDamageContext(
            BattleUnitState user,
            BattleUnitState target,
            PachimonAttribute attribute)
        {
            return CreateDamageContext(state: null, user, target, attribute);
        }

        private DamageContext CreateDamageContext(
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
                    AquaShockMath.CalculateElectricBaseDamage(
                        _skill,
                        attributeValue),
                PachimonAttribute.Aqua =>
                    AquaShockMath.CalculateAquaBaseDamage(
                        _skill,
                        attributeValue,
                        state?.ResolveAttributeRatio(
                            PachimonAttribute.Aqua,
                            100m) ?? 100m),
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

        private BattleDamageApplicationResult ResolveComponent(
            SkillExecutionContext context,
            BattleUnitState target,
            PachimonAttribute attribute,
            SkillHit hit)
        {
            return BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                CreateDamageContext(context.State, context.User, target, attribute),
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
                    "Aqua Shock Logic received another Skill Asset.",
                    nameof(context));
            }
        }
    }
}
