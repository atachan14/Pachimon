using System;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattleDamageApplicationResult
    {
        public BattleDamageApplicationResult(
            DamageCalculationResult calculation,
            int finalDamage,
            int appliedDamage,
            int shieldAbsorbedDamage,
            BattleUnitState actualTarget)
        {
            Calculation = calculation
                ?? throw new ArgumentNullException(nameof(calculation));
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
            ShieldAbsorbedDamage = shieldAbsorbedDamage;
            ActualTarget = actualTarget
                ?? throw new ArgumentNullException(nameof(actualTarget));
        }

        public DamageCalculationResult Calculation { get; }
        public int FinalDamage { get; }
        public int AppliedDamage { get; }
        public int ShieldAbsorbedDamage { get; }
        public BattleUnitState ActualTarget { get; }
        public int DamageAfterShield => FinalDamage - ShieldAbsorbedDamage;
    }

    public static class BattleAttributeDamageService
    {
        public static BattleDamageApplicationResult Apply(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageContext damageContext)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (damageContext == null)
            {
                throw new ArgumentNullException(nameof(damageContext));
            }
            if (source == null
                && (damageContext.OriginKind != DamageOriginKind.Status
                    || damageContext.IsAttack
                    || damageContext.ApplyAttackerAttributeMultiplier
                    || damageContext.ApplyDamageBonusMultiplier
                    || damageContext.ApplyOutgoingModifiers))
            {
                throw new ArgumentNullException(
                    nameof(source),
                    "Source-less damage must be an unmodified Status damage.");
            }

            var actualTarget = state.Statuses.ResolveAttackTarget(
                source,
                target,
                damageContext.IsAttack);
            if (!ReferenceEquals(actualTarget, target))
            {
                target = actualTarget;
                damageContext = damageContext.WithDefenderStats(
                    target.GetBattleStats());
            }

            if (source != null && damageContext.ApplyOutgoingModifiers)
            {
                damageContext = damageContext.WithPenetrationPercent(
                    state.Passives.ModifyPenetrationPercent(
                        state,
                        source,
                        target,
                        damageContext));
            }
            var calculation = AttributeDamageCalculator.Calculate(damageContext);
            if (damageContext.IsAttack
                && state.Statuses.TryEvadeAttack(
                    source,
                    target,
                    damageContext.OriginKind,
                    damageContext.OriginId))
            {
                return new BattleDamageApplicationResult(
                    calculation,
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target);
            }
            var beforeDamage = new BeforeAttributeDamageEvent(
                state,
                source,
                target,
                calculation);
            if (damageContext.ApplyAttackerAttributeMultiplier)
            {
                var statType = PachimonStatTypeUtility.FromAttribute(
                    damageContext.Attribute);
                var normalMultiplier = calculation.AttackerAttributeMultiplier;
                var effectiveRatio = state.ResolveAttributeRatio(
                    damageContext.Attribute,
                    100m);
                var weatherMultiplier = SignedStatMath.AmplificationMultiplier(
                    source.GetBattleStatValue(statType)
                    * effectiveRatio / 100m);
                beforeDamage.MultiplyDamage(weatherMultiplier / normalMultiplier);
            }
            state.Events.Publish(beforeDamage);
            state.Statuses.ApplyIncomingDamageModifiers(beforeDamage);
            var interception = damageContext.IsAttack
                ? state.Fields.InterceptAttributeAttack(
                    source,
                    target,
                    damageContext.Attribute,
                    calculation.PreDefenseDamage
                        * beforeDamage.OutgoingMultiplier)
                : default;
            if (interception.WasIntercepted
                && interception.OverflowDamage <= 0)
            {
                return new BattleDamageApplicationResult(
                    calculation,
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target);
            }

            var targetUnroundedDamage = interception.WasIntercepted
                ? interception.OverflowDamage
                    * calculation.DefenderAttributeMultiplier
                    * calculation.ResistBonusMultiplier
                : beforeDamage.UnroundedDamage;
            targetUnroundedDamage = state.Fields
                .ApplyIncomingAttributeDamageReduction(
                    target,
                    damageContext.Attribute,
                    targetUnroundedDamage);
            var finalDamage = AttributeDamageCalculator.FinalizeNormalDamage(
                targetUnroundedDamage);
            var shield = target.AbsorbDamage(finalDamage);
            var hpBefore = target.CurrentHp;
            var appliedDamage = target.ApplyDamage(shield.RemainingDamage);
            state.Presentation.RecordDamage(
                target,
                hpBefore,
                target.CurrentHp,
                appliedDamage,
                isTrueDamage: false,
                shield.AbsorbedDamage);
            var appliedEvent = new AttributeDamageAppliedEvent(
                state,
                source,
                target,
                calculation,
                interception.WasIntercepted
                    ? interception.OverflowDamage
                    : calculation.PreDefenseDamage
                        * beforeDamage.OutgoingMultiplier,
                finalDamage,
                appliedDamage,
                shield.AbsorbedDamage);
            state.Events.Publish(appliedEvent);
            PublishAttackReceived(
                state,
                source,
                target,
                damageContext.OriginKind,
                damageContext.OriginId,
                damageContext.IsAttack,
                isTrueDamage: false,
                damageContext.Attribute,
                finalDamage,
                appliedDamage,
                shield.AbsorbedDamage);
            state.Statuses.HandleAttributeDamageApplied(appliedEvent);
            var damageAppliedEvent = new DamageAppliedEvent(
                state,
                source,
                target,
                damageContext.OriginKind,
                damageContext.OriginId,
                isTrueDamage: false,
                damageContext.Attribute,
                finalDamage,
                appliedDamage,
                shield.AbsorbedDamage);
            state.Events.Publish(damageAppliedEvent);
            state.Statuses.HandleDamageApplied(damageAppliedEvent);
            state.Weather.HandleDamageApplied(damageAppliedEvent);
            return new BattleDamageApplicationResult(
                calculation,
                finalDamage,
                appliedDamage,
                shield.AbsorbedDamage,
                target);
        }

        internal static void PublishAttackReceived(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageOriginKind originKind,
            int originId,
            bool isAttack,
            bool isTrueDamage,
            PachimonAttribute? attribute,
            int finalDamage,
            int appliedDamage,
            int shieldAbsorbedDamage = 0)
        {
            if (!isAttack)
            {
                return;
            }

            state.Events.Publish(new AttackReceivedEvent(
                state,
                source,
                target,
                originKind,
                originId,
                isTrueDamage,
                attribute,
                finalDamage,
                appliedDamage,
                shieldAbsorbedDamage));
        }
    }

    public sealed class BattleTrueDamageApplicationResult
    {
        public BattleTrueDamageApplicationResult(
            int finalDamage,
            int appliedDamage,
            int shieldAbsorbedDamage,
            BattleUnitState actualTarget)
        {
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
            ShieldAbsorbedDamage = shieldAbsorbedDamage;
            ActualTarget = actualTarget
                ?? throw new ArgumentNullException(nameof(actualTarget));
        }

        public int FinalDamage { get; }
        public int AppliedDamage { get; }
        public int ShieldAbsorbedDamage { get; }
        public BattleUnitState ActualTarget { get; }
        public int DamageAfterShield => FinalDamage - ShieldAbsorbedDamage;
    }

    public static class BattleTrueDamageService
    {
        public static BattleTrueDamageApplicationResult Apply(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            TrueDamageContext damageContext)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (damageContext == null)
            {
                throw new ArgumentNullException(nameof(damageContext));
            }

            target = state.Statuses.ResolveAttackTarget(
                source,
                target,
                damageContext.IsAttack);

            if (damageContext.IsAttack
                && state.Statuses.TryEvadeAttack(
                    source,
                    target,
                    damageContext.OriginKind,
                    damageContext.OriginId))
            {
                return new BattleTrueDamageApplicationResult(
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target);
            }

            var interception = damageContext.IsAttack
                ? state.Fields.InterceptTrueAttack(
                    source,
                    target,
                    damageContext.Damage)
                : default;
            if (interception.WasIntercepted
                && interception.OverflowDamage <= 0)
            {
                return new BattleTrueDamageApplicationResult(
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target);
            }

            var targetDamage = interception.WasIntercepted
                ? interception.OverflowDamage
                : damageContext.Damage;
            var shield = target.AbsorbDamage(targetDamage);
            var hpBefore = target.CurrentHp;
            var appliedDamage = target.ApplyDamage(shield.RemainingDamage);
            state.Presentation.RecordDamage(
                target,
                hpBefore,
                target.CurrentHp,
                appliedDamage,
                isTrueDamage: true,
                shield.AbsorbedDamage);
            BattleAttributeDamageService.PublishAttackReceived(
                state,
                source,
                target,
                damageContext.OriginKind,
                damageContext.OriginId,
                damageContext.IsAttack,
                isTrueDamage: true,
                attribute: null,
                targetDamage,
                appliedDamage,
                shield.AbsorbedDamage);
            var damageAppliedEvent = new DamageAppliedEvent(
                state,
                source,
                target,
                damageContext.OriginKind,
                damageContext.OriginId,
                isTrueDamage: true,
                attribute: null,
                targetDamage,
                appliedDamage,
                shield.AbsorbedDamage);
            state.Events.Publish(damageAppliedEvent);
            state.Statuses.HandleDamageApplied(damageAppliedEvent);
            state.Weather.HandleDamageApplied(damageAppliedEvent);
            return new BattleTrueDamageApplicationResult(
                targetDamage,
                appliedDamage,
                shield.AbsorbedDamage,
                target);
        }
    }

    public static class BattleStatusDamageService
    {
        public static BattleDamageApplicationResult ApplyAttribute(
            BattleState state,
            BattleUnitState target,
            BattleStatusId statusId,
            PachimonAttribute attribute,
            decimal baseDamage)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (baseDamage < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDamage));
            }

            var targetStats = target.GetBattleStats();
            var result = BattleAttributeDamageService.Apply(
                state,
                source: null,
                target,
                new DamageContext(
                    DamageOriginKind.Status,
                    (int)statusId,
                    baseDamage,
                    targetStats,
                    targetStats,
                    attribute,
                    isAttack: false,
                    applyAttackerAttributeMultiplier: false,
                    penetrationPercent: 0m,
                    applyDamageBonusMultiplier: false,
                    applyOutgoingModifiers: false));
            state.Events.Publish(new StatusDamageAppliedEvent(
                state,
                target,
                statusId,
                attribute,
                result.FinalDamage,
                result.AppliedDamage,
                result.ShieldAbsorbedDamage));
            return result;
        }

        public static decimal CalculateUnrounded(
            decimal baseDamage,
            BattleUnitState target,
            PachimonAttribute attribute)
        {
            if (baseDamage < 0m) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var stats = target.GetBattleStats();
            var attributeStat = PachimonStatTypeUtility.FromAttribute(attribute);
            return baseDamage
                * SignedStatMath.ReductionMultiplier(stats.GetValue(attributeStat))
                * SignedStatMath.ReductionMultiplier(stats.ResistBonus);
        }

        public static int Apply(
            BattleState state,
            BattleUnitState target,
            BattleStatusId statusId,
            PachimonAttribute attribute,
            int damage)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));

            var shield = target.AbsorbDamage(damage);
            var hpBefore = target.CurrentHp;
            var appliedDamage = target.ApplyDamage(shield.RemainingDamage);
            state.ToxinPresentation.RecordDamage(
                target,
                hpBefore,
                target.CurrentHp);
            state.Events.Publish(new StatusDamageAppliedEvent(
                state,
                target,
                statusId,
                attribute,
                damage,
                appliedDamage,
                shield.AbsorbedDamage));
            state.Events.Publish(new DamageAppliedEvent(
                state,
                source: null,
                target,
                DamageOriginKind.Status,
                (int)statusId,
                isTrueDamage: false,
                attribute,
                damage,
                appliedDamage,
                shield.AbsorbedDamage));
            if (hpBefore > 0 && target.IsDefeated)
            {
                state.Events.Publish(new UnitDefeatedEvent(
                    state,
                    source: null,
                    defeatedUnit: target));
            }
            return appliedDamage;
        }
    }
}
