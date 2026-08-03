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
                        target,
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

    public sealed class ElectricShockSkillLogic : ISkillLogic
    {
        private readonly BasicAttributeDamageSkillLogic _damageLogic =
            new(PachimonAttribute.Electric);

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var resolution = _damageLogic.Resolve(context);
            var target = BasicAttributeDamageSkillLogic.GetFrontTarget(context);
            context.State.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Paralysis,
                    BattleStatusCategory.Slow,
                    context.User,
                    ElectricShockMath.CalculateSlowValue(context.User)));
            return resolution;
        }
    }

    public static class ElectricShockMath
    {
        public const int ElectricBaseValue = 50;
        public const int IceBaseValue = 25;

        public static int CalculateSlowValue(BattleUnitState user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var electric = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    ElectricBaseValue,
                    user.GetBattleStatValue(PachimonStatType.Electric)));
            var ice = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    IceBaseValue,
                    user.GetBattleStatValue(PachimonStatType.Ice)));
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
            var target = BasicAttributeDamageSkillLogic.GetFrontTarget(context);
            context.State.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Chill,
                    BattleStatusCategory.Slow,
                    context.User,
                    ColdHandMath.CalculateChillValue(context.User)));
            return resolution;
        }
    }

    public static class ColdHandMath
    {
        public const int IceBaseValue = 75;

        public static int CalculateChillValue(BattleUnitState user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            return SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    IceBaseValue,
                    user.GetBattleStatValue(PachimonStatType.Ice)));
        }
    }
}
