using System;
using System.Collections.Generic;
using System.Linq;
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
            BattleUnitState actualTarget,
            bool wasEvaded = false,
            SkillHit hit = null)
        {
            Calculation = calculation
                ?? throw new ArgumentNullException(nameof(calculation));
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
            ShieldAbsorbedDamage = shieldAbsorbedDamage;
            ActualTarget = actualTarget
                ?? throw new ArgumentNullException(nameof(actualTarget));
            Hit = hit;
            WasEvaded = hit?.WasEvaded ?? wasEvaded;
        }

        public DamageCalculationResult Calculation { get; }
        public int FinalDamage { get; }
        public int AppliedDamage { get; }
        public int ShieldAbsorbedDamage { get; }
        public BattleUnitState ActualTarget { get; }
        public bool WasEvaded { get; }
        public SkillHit Hit { get; }
        public int DamageAfterShield => FinalDamage - ShieldAbsorbedDamage;
    }

    public static class BattleAttributeDamageService
    {
        public static BattleDamageApplicationResult Apply(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageContext damageContext,
            SkillHit hit = null)
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

            if (hit == null && damageContext.IsAttack)
            {
                hit = new SkillHit(
                    state,
                    source,
                    target,
                    damageContext.OriginKind,
                    damageContext.OriginId);
            }
            hit?.Validate(state, source, target);
            var actualTarget = hit?.Target ?? target;
            if (!ReferenceEquals(actualTarget, target))
            {
                target = actualTarget;
                damageContext = damageContext.WithDefenderStats(
                    target.GetBattleStats());
            }

            if (!target.IsAlive)
            {
                return new BattleDamageApplicationResult(
                    AttributeDamageCalculator.Calculate(damageContext),
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target,
                    hit: hit);
            }

            if (source != null && damageContext.ApplyOutgoingModifiers)
            {
                damageContext = damageContext.WithPenetration(
                    state.Passives.ModifyPenetration(
                        state,
                        source,
                        target,
                        damageContext));
            }
            if (source != null)
            {
                var statType = PachimonStatTypeUtility.FromAttribute(
                    damageContext.Attribute);
                damageContext = damageContext.WithAttackerAttributeValue(
                    source.GetBattleStatValue(statType)
                    * state.ResolveAttributeRatio(
                        damageContext.Attribute,
                        100m)
                    / 100m);
            }
            var calculation = AttributeDamageCalculator.Calculate(damageContext);
            var wasEvaded = hit?.WasEvaded ?? false;
            if (wasEvaded)
            {
                return new BattleDamageApplicationResult(
                    calculation,
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target,
                    hit: hit);
            }
            var beforeDamage = new BeforeAttributeDamageEvent(
                state,
                source,
                target,
                calculation,
                hit);
            state.Events.Publish(beforeDamage);
            state.Statuses.ApplyIncomingDamageModifiers(beforeDamage);
            if (damageContext.IsAttack
                && state.Fields.TryEvadeSkillAttack(
                    source,
                    target,
                    damageContext.OriginKind,
                    calculation.PreDefenseDamage
                        * beforeDamage.OutgoingMultiplier,
                    hit))
            {
                return new BattleDamageApplicationResult(
                    calculation,
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target,
                    hit: hit);
            }
            state.Weather.HandleAttributeDamage(
                source,
                target,
                damageContext.Attribute,
                calculation.PreDefenseDamage * beforeDamage.OutgoingMultiplier);
            var interception = damageContext.IsAttack
                ? state.Fields.InterceptAttributeAttack(
                    source,
                    target,
                    damageContext.Attribute,
                    calculation.PreDefenseDamage
                        * beforeDamage.OutgoingMultiplier,
                    damageContext.OriginKind,
                    damageContext.OriginId)
                : default;
            hit?.RecordDamageInterception(
                interception.FieldEffect,
                !interception.WasIntercepted
                || interception.OverflowDamage > 0);
            if (interception.WasIntercepted
                && interception.OverflowDamage <= 0)
            {
                return new BattleDamageApplicationResult(
                    calculation,
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target,
                    hit: hit);
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
            var finalDamage = state.Statuses.ClampIncomingDamage(
                target,
                AttributeDamageCalculator.FinalizeNormalDamage(
                    targetUnroundedDamage));
            var activeShieldOrders = target.Shields
                .Select(current => current.ApplicationOrder)
                .ToArray();
            var statusesBeforeDamage = CaptureStatusValues(target);
            var leakValueBeforeDamage = target.Statuses
                .Where(status =>
                    (status.Categories & BattleStatusCategory.Leak) != 0)
                .Sum(status => checked(status.Value * status.StackCount));
            var shield = target.AbsorbDamage(finalDamage);
            var hpBefore = target.CurrentHp;
            var appliedDamage = target.ApplyDamage(shield.RemainingDamage);
            state.Presentation.RecordDamage(
                target,
                hpBefore,
                target.CurrentHp,
                appliedDamage,
                isTrueDamage: false,
                shieldAbsorbedDamage: shield.AbsorbedDamage,
                attribute: damageContext.Attribute);
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
            if (damageContext.Attribute == PachimonAttribute.Electric
                && appliedDamage + shield.AbsorbedDamage > 0)
            {
                state.RecordElectricDamage();
            }
            state.Events.Publish(appliedEvent);
            state.Fields.HandleAttributeDamageApplied(appliedEvent);
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
                shield.AbsorbedDamage,
                activeShieldOrders);
            state.Statuses.HandleAttributeDamageApplied(
                appliedEvent,
                leakValueBeforeDamage);
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
                shield.AbsorbedDamage,
                statusesBeforeDamage,
                damageContext.Tags);
            state.Events.Publish(damageAppliedEvent);
            state.Statuses.HandleDamageApplied(damageAppliedEvent);
            state.Weather.HandleDamageApplied(damageAppliedEvent);
            return new BattleDamageApplicationResult(
                calculation,
                finalDamage,
                appliedDamage,
                shield.AbsorbedDamage,
                target,
                hit: hit);
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
            int shieldAbsorbedDamage = 0,
            IReadOnlyCollection<long> activeShieldApplicationOrders = null)
        {
            if (!isAttack)
            {
                return;
            }

            var attackEvent = new AttackReceivedEvent(
                state,
                source,
                target,
                originKind,
                originId,
                isTrueDamage,
                attribute,
                finalDamage,
                appliedDamage,
                shieldAbsorbedDamage,
                activeShieldApplicationOrders);
            state.Events.Publish(attackEvent);
            state.Statuses.HandleAttackReceived(attackEvent);
        }

        internal static IReadOnlyDictionary<BattleStatusId, int>
            CaptureStatusValues(BattleUnitState target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            // Timed statuses such as Stun may coexist as independent instances.
            return target.Statuses
                .GroupBy(status => status.StatusId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(status => status.Value));
        }
    }

    public sealed class BattleTrueDamageApplicationResult
    {
        public BattleTrueDamageApplicationResult(
            int finalDamage,
            int appliedDamage,
            int shieldAbsorbedDamage,
            BattleUnitState actualTarget,
            bool wasEvaded = false,
            SkillHit hit = null)
        {
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
            ShieldAbsorbedDamage = shieldAbsorbedDamage;
            ActualTarget = actualTarget
                ?? throw new ArgumentNullException(nameof(actualTarget));
            Hit = hit;
            WasEvaded = hit?.WasEvaded ?? wasEvaded;
        }

        public int FinalDamage { get; }
        public int AppliedDamage { get; }
        public int ShieldAbsorbedDamage { get; }
        public BattleUnitState ActualTarget { get; }
        public bool WasEvaded { get; }
        public SkillHit Hit { get; }
        public int DamageAfterShield => FinalDamage - ShieldAbsorbedDamage;
    }

    public static class BattleTrueDamageService
    {
        public static BattleTrueDamageApplicationResult Apply(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            TrueDamageContext damageContext,
            SkillHit hit = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (damageContext == null)
            {
                throw new ArgumentNullException(nameof(damageContext));
            }

            if (hit == null && damageContext.IsAttack)
            {
                hit = new SkillHit(
                    state,
                    source,
                    target,
                    damageContext.OriginKind,
                    damageContext.OriginId);
            }
            hit?.Validate(state, source, target);
            target = hit?.Target ?? target;

            if (!target.IsAlive)
            {
                return new BattleTrueDamageApplicationResult(
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target,
                    hit: hit);
            }

            var wasEvaded = hit?.WasEvaded ?? false;
            if (wasEvaded)
            {
                return new BattleTrueDamageApplicationResult(
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target,
                    hit: hit);
            }

            if (damageContext.IsAttack
                && state.Fields.TryEvadeSkillAttack(
                    source,
                    target,
                    damageContext.OriginKind,
                    damageContext.Damage,
                    hit))
            {
                return new BattleTrueDamageApplicationResult(
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target,
                    hit: hit);
            }

            var interception = damageContext.IsAttack
                ? state.Fields.InterceptTrueAttack(
                    source,
                    target,
                    damageContext.Damage,
                    damageContext.OriginKind,
                    damageContext.OriginId)
                : default;
            hit?.RecordDamageInterception(
                interception.FieldEffect,
                !interception.WasIntercepted
                || interception.OverflowDamage > 0);
            if (interception.WasIntercepted
                && interception.OverflowDamage <= 0)
            {
                return new BattleTrueDamageApplicationResult(
                    finalDamage: 0,
                    appliedDamage: 0,
                    shieldAbsorbedDamage: 0,
                    actualTarget: target,
                    hit: hit);
            }

            var targetDamage = interception.WasIntercepted
                ? interception.OverflowDamage
                : damageContext.Damage;
            targetDamage = state.Statuses.ClampIncomingDamage(
                target,
                targetDamage);
            var activeShieldOrders = target.Shields
                .Select(current => current.ApplicationOrder)
                .ToArray();
            var statusesBeforeDamage = BattleAttributeDamageService
                .CaptureStatusValues(target);
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
                shield.AbsorbedDamage,
                activeShieldOrders);
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
                shield.AbsorbedDamage,
                statusesBeforeDamage,
                damageContext.Tags);
            state.Events.Publish(damageAppliedEvent);
            state.Statuses.HandleDamageApplied(damageAppliedEvent);
            state.Weather.HandleDamageApplied(damageAppliedEvent);
            return new BattleTrueDamageApplicationResult(
                targetDamage,
                appliedDamage,
                shield.AbsorbedDamage,
                target,
                hit: hit);
        }
    }

    public static class BattleStatusDamageService
    {
        public static BattleDamageApplicationResult ApplyAttribute(
            BattleState state,
            BattleUnitState target,
            BattleStatusId statusId,
            PachimonAttribute attribute,
            decimal baseDamage,
            DamageTag tags = DamageTag.None)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (baseDamage < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDamage));
            }

            var targetWasAlive = target.IsAlive;
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
                    applyDamageBonusMultiplier: false,
                    applyOutgoingModifiers: false,
                    tags: tags));
            if (targetWasAlive)
            {
                state.Events.Publish(new StatusDamageAppliedEvent(
                    state,
                    target,
                    statusId,
                    attribute,
                    result.FinalDamage,
                    result.AppliedDamage,
                    result.ShieldAbsorbedDamage));
            }
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
            int damage,
            DamageTag tags = DamageTag.None)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (!target.IsAlive) return 0;

            damage = state.Statuses.ClampIncomingDamage(target, damage);
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
                shield.AbsorbedDamage,
                tags: tags));
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

    public static class BattleExecutionDamageService
    {
        public static int Execute(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            int passiveId)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (passiveId <= 0) throw new ArgumentOutOfRangeException(nameof(passiveId));
            if (!target.IsAlive) return 0;

            var hpBefore = target.CurrentHp;
            var incomingDamage = state.Statuses.ClampIncomingDamage(
                target,
                hpBefore);
            var damage = target.ApplyDamage(incomingDamage);
            state.Presentation.RecordDamage(
                target,
                hpBefore,
                target.CurrentHp,
                damage,
                isTrueDamage: true,
                shieldAbsorbedDamage: 0);
            state.Events.Publish(new DamageAppliedEvent(
                state,
                source,
                target,
                DamageOriginKind.Passive,
                passiveId,
                isTrueDamage: true,
                attribute: null,
                finalDamage: damage,
                appliedDamage: damage,
                shieldAbsorbedDamage: 0));
            state.AddLog($"{target.DisplayName}はラストタッチで戦闘不能になった！");
            return damage;
        }
    }
}
