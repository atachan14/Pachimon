using System;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class BattleDamageApplicationResult
    {
        public BattleDamageApplicationResult(
            DamageCalculationResult calculation,
            int finalDamage,
            int appliedDamage)
        {
            Calculation = calculation
                ?? throw new ArgumentNullException(nameof(calculation));
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
        }

        public DamageCalculationResult Calculation { get; }
        public int FinalDamage { get; }
        public int AppliedDamage { get; }
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
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (damageContext == null)
            {
                throw new ArgumentNullException(nameof(damageContext));
            }

            var calculation = AttributeDamageCalculator.Calculate(damageContext);
            var beforeDamage = new BeforeAttributeDamageEvent(
                state,
                source,
                target,
                calculation);
            state.Events.Publish(beforeDamage);
            var finalDamage = AttributeDamageCalculator.FinalizeNormalDamage(
                beforeDamage.UnroundedDamage);
            var hpBefore = target.CurrentHp;
            var appliedDamage = target.ApplyDamage(finalDamage);
            state.Presentation.RecordDamage(
                target,
                hpBefore,
                target.CurrentHp,
                appliedDamage,
                isTrueDamage: false);
            var appliedEvent = new AttributeDamageAppliedEvent(
                state,
                source,
                target,
                calculation,
                finalDamage,
                appliedDamage);
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
                appliedDamage);
            state.Statuses.HandleAttributeDamageApplied(appliedEvent);
            return new BattleDamageApplicationResult(
                calculation,
                finalDamage,
                appliedDamage);
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
            int appliedDamage)
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
                appliedDamage));
        }
    }

    public sealed class BattleTrueDamageApplicationResult
    {
        public BattleTrueDamageApplicationResult(
            int finalDamage,
            int appliedDamage)
        {
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
        }

        public int FinalDamage { get; }
        public int AppliedDamage { get; }
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

            var hpBefore = target.CurrentHp;
            var appliedDamage = target.ApplyDamage(damageContext.Damage);
            state.Presentation.RecordDamage(
                target,
                hpBefore,
                target.CurrentHp,
                appliedDamage,
                isTrueDamage: true);
            BattleAttributeDamageService.PublishAttackReceived(
                state,
                source,
                target,
                damageContext.OriginKind,
                damageContext.OriginId,
                damageContext.IsAttack,
                isTrueDamage: true,
                attribute: null,
                damageContext.Damage,
                appliedDamage);
            return new BattleTrueDamageApplicationResult(
                damageContext.Damage,
                appliedDamage);
        }
    }
}
