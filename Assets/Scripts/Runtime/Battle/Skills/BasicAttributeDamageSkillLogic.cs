using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class BasicAttributeDamageSkillLogic : ISkillLogic
    {
        public const int DefaultBaseDamage = 200;
        private readonly InitialAttributeDamageSkillAsset _skill;
        private readonly PachimonAttribute _attribute;

        public BasicAttributeDamageSkillLogic(PachimonAttribute attribute)
        {
            _attribute = attribute;
        }

        public BasicAttributeDamageSkillLogic(
            InitialAttributeDamageSkillAsset skill,
            PachimonAttribute attribute)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
            _attribute = attribute;
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (_skill != null && !ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Initial Attribute Skill Logic received another Skill Asset.",
                    nameof(context));
            }
            var target = GetFrontTarget(context);
            var hit = context.BeginAttackHit(target);
            var baseDamage = _skill?.BaseDamage
                ?? (context.Skill as Pachimon.Skills.PlaceholderSkillAsset)
                    ?.BaseDamage
                ?? DefaultBaseDamage;
            var damageRatio = _skill?.DamageRatio ?? 100;
            var damage = context.ScaleFromAttribute(
                baseDamage,
                _attribute,
                damageRatio);
            var result = BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    context.Skill.SkillId,
                    damage,
                    context.User.GetBattleStats(),
                    target.GetBattleStats(),
                    _attribute,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false),
                hit);
            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        result.ActualTarget,
                        result.AppliedDamage,
                        isTrueDamage: false,
                        hit: hit),
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
        private readonly PoisonNeedleSkillAsset _skill;

        public PoisonNeedleSkillLogic()
        {
        }

        public PoisonNeedleSkillLogic(PoisonNeedleSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
            _damageLogic = new BasicAttributeDamageSkillLogic(
                skill,
                PachimonAttribute.Poison);
        }

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
            var baseValue = _skill?.ToxinBaseValue
                ?? (placeholder?.StatusBaseValue > 0
                    ? placeholder.StatusBaseValue
                    : DefaultStatusBaseValue);
            var scalingPercent = _skill?.ToxinRatio
                ?? placeholder?.StatusScalingPercent
                ?? DefaultStatusScalingPercent;
            var toxinValue = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    baseValue,
                    context.GetAttributeValue(PachimonAttribute.Poison),
                    context.GetAttributeRatio(
                        PachimonAttribute.Poison,
                        scalingPercent)));
            if (toxinValue > 0)
            {
                resolution.Effects[0].Hit.ApplyStatus(
                    BattleStatusFactory.CreateToxin(
                        context.User,
                        toxinValue,
                        _skill?.ToxinStatus
                            ?? placeholder?.ToxinStatus
                            ?? throw new InvalidOperationException(
                                "Poison Needle requires a Toxin Definition.")));
            }

            return resolution;
        }
    }

    public sealed class ElectricShockSkillLogic : ISkillLogic
    {
        private readonly BasicAttributeDamageSkillLogic _damageLogic;
        private readonly ElectricShockSkillAsset _skill;

        public ElectricShockSkillLogic()
        {
            _damageLogic = new BasicAttributeDamageSkillLogic(
                PachimonAttribute.Electric);
        }

        public ElectricShockSkillLogic(ElectricShockSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
            _damageLogic = new BasicAttributeDamageSkillLogic(
                skill,
                PachimonAttribute.Electric);
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var resolution = _damageLogic.Resolve(context);
            var target = resolution.Effects[0].Target;
            resolution.Effects[0].Hit.ApplyStatus(
                BattleStatusFactory.CreateSlow(
                    context.User,
                    ElectricShockMath.CalculateSlowValue(
                        context.State,
                        context.User,
                        _skill),
                    _skill?.ParalysisStatus
                    ?? (context.Skill as Pachimon.Skills.PlaceholderSkillAsset)
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
            BattleUnitState user,
            ElectricShockSkillAsset skill = null)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var electric = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    skill?.ElectricParalysisBaseValue ?? ElectricBaseValue,
                    user.GetBattleStatValue(PachimonStatType.Electric),
                    state?.ResolveAttributeRatio(
                        PachimonAttribute.Electric,
                        skill?.ElectricParalysisRatio ?? 100m)
                    ?? skill?.ElectricParalysisRatio
                    ?? 100m));
            var ice = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    skill?.IceParalysisBaseValue ?? IceBaseValue,
                    user.GetBattleStatValue(PachimonStatType.Ice),
                    state?.ResolveAttributeRatio(
                        PachimonAttribute.Ice,
                        skill?.IceParalysisRatio ?? 100m)
                    ?? skill?.IceParalysisRatio
                    ?? 100m));
            return checked(electric + ice);
        }
    }

    public sealed class ColdHandSkillLogic : ISkillLogic
    {
        private readonly BasicAttributeDamageSkillLogic _damageLogic;
        private readonly ColdHandSkillAsset _skill;

        public ColdHandSkillLogic()
        {
            _damageLogic = new BasicAttributeDamageSkillLogic(
                PachimonAttribute.Ice);
        }

        public ColdHandSkillLogic(ColdHandSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
            _damageLogic = new BasicAttributeDamageSkillLogic(
                skill,
                PachimonAttribute.Ice);
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var resolution = _damageLogic.Resolve(context);
            var target = resolution.Effects[0].Target;
            resolution.Effects[0].Hit.ApplyStatus(
                BattleStatusFactory.CreateSlow(
                    context.User,
                    ColdHandMath.CalculateChillValue(
                        context.State,
                        context.User,
                        _skill),
                    _skill?.ChillStatus
                    ?? (context.Skill as Pachimon.Skills.PlaceholderSkillAsset)
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
            BattleUnitState user,
            ColdHandSkillAsset skill = null)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            return SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    skill?.ChillBaseValue ?? IceBaseValue,
                    user.GetBattleStatValue(PachimonStatType.Ice),
                    state?.ResolveAttributeRatio(
                        PachimonAttribute.Ice,
                        skill?.ChillRatio ?? 100m)
                    ?? skill?.ChillRatio
                    ?? 100m));
        }
    }
}
