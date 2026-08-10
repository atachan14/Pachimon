using System;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BasicAttributeDamageSkillLogic : ISkillLogic
    {
        public const int BaseDamage = 100;
        private readonly PachimonAttribute _attribute;

        public BasicAttributeDamageSkillLogic(PachimonAttribute attribute)
        {
            _attribute = attribute;
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var target = GetFrontTarget(context);
            var result = BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    context.Skill.SkillId,
                    BaseDamage,
                    context.User.GetBattleStats(),
                    target.GetBattleStats(),
                    _attribute,
                    isAttack: true));
            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        result.ActualTarget,
                        result.AppliedDamage,
                        isTrueDamage: false),
                });
        }

        internal static BattleUnitState GetFrontTarget(
            SkillExecutionContext context)
        {
            return context.Targets.GetFrontEnemy()
                ?? throw new InvalidOperationException("No living Enemy target was found.");
        }

    }

    public sealed class PoisonNeedleSkillLogic : ISkillLogic
    {
        private const int DefaultStatusBaseValue = 100;
        private const int DefaultStatusScalingPercent = 100;
        private readonly BasicAttributeDamageSkillLogic _damageLogic =
            new(PachimonAttribute.Poison);

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var resolution = _damageLogic.Resolve(context);
            var target = resolution.Effects[0].Target;
            if (!target.IsAlive)
            {
                return resolution;
            }

            var placeholder = context.Skill as Pachimon.Skills.PlaceholderSkillAsset;
            var baseValue = placeholder?.StatusBaseValue > 0
                ? placeholder.StatusBaseValue
                : DefaultStatusBaseValue;
            var scalingPercent = placeholder?.StatusScalingPercent ??
                DefaultStatusScalingPercent;
            var toxinValue = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    baseValue,
                    context.GetAttributeValue(PachimonAttribute.Poison),
                    context.GetAttributeRatio(
                        PachimonAttribute.Poison,
                        scalingPercent)));
            if (toxinValue > 0)
            {
                context.State.Statuses.ApplyAttackStatus(
                    target,
                    BattleStatusFactory.CreateToxin(
                        context.User,
                        toxinValue,
                        placeholder?.ToxinStatus
                            ?? throw new InvalidOperationException(
                                "Poison Needle requires a Toxin Definition.")));
            }

            return resolution;
        }
    }

    public sealed class ElectricShockSkillLogic : ISkillLogic
    {
        private readonly BasicAttributeDamageSkillLogic _damageLogic =
            new(PachimonAttribute.Electric);

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var resolution = _damageLogic.Resolve(context);
            var target = resolution.Effects[0].Target;
            context.State.Statuses.ApplyAttackStatus(
                target,
                BattleStatusFactory.CreateSlow(
                    context.User,
                    ElectricShockMath.CalculateSlowValue(
                        context.State,
                        context.User),
                    (context.Skill as Pachimon.Skills.PlaceholderSkillAsset)
                        ?.ParalysisStatus
                    ?? throw new InvalidOperationException(
                        "Electric Shock requires a Paralysis Definition.")));
            return resolution;
        }
    }

    public static class ElectricShockMath
    {
        public const int ElectricBaseValue = 50;
        public const int IceBaseValue = 25;

        public static int CalculateSlowValue(BattleUnitState user)
        {
            return CalculateSlowValue(state: null, user);
        }

        public static int CalculateSlowValue(
            BattleState state,
            BattleUnitState user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var electric = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    ElectricBaseValue,
                    user.GetBattleStatValue(PachimonStatType.Electric),
                    state?.ResolveAttributeRatio(
                        PachimonAttribute.Electric,
                        100m) ?? 100m));
            var ice = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    IceBaseValue,
                    user.GetBattleStatValue(PachimonStatType.Ice),
                    state?.ResolveAttributeRatio(
                        PachimonAttribute.Ice,
                        100m) ?? 100m));
            return checked(electric + ice);
        }
    }

    public sealed class ColdHandSkillLogic : ISkillLogic
    {
        private readonly BasicAttributeDamageSkillLogic _damageLogic =
            new(PachimonAttribute.Ice);

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var resolution = _damageLogic.Resolve(context);
            var target = resolution.Effects[0].Target;
            context.State.Statuses.ApplyAttackStatus(
                target,
                BattleStatusFactory.CreateSlow(
                    context.User,
                    ColdHandMath.CalculateChillValue(
                        context.State,
                        context.User),
                    (context.Skill as Pachimon.Skills.PlaceholderSkillAsset)
                        ?.ChillStatus
                    ?? throw new InvalidOperationException(
                        "Cold Hand requires a Chill Definition.")));
            return resolution;
        }
    }

    public static class ColdHandMath
    {
        public const int IceBaseValue = 75;

        public static int CalculateChillValue(BattleUnitState user)
        {
            return CalculateChillValue(state: null, user);
        }

        public static int CalculateChillValue(
            BattleState state,
            BattleUnitState user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            return SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    IceBaseValue,
                    user.GetBattleStatValue(PachimonStatType.Ice),
                    state?.ResolveAttributeRatio(
                        PachimonAttribute.Ice,
                        100m) ?? 100m));
        }
    }
}
