using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Battle;
using Pachimon.Run;

namespace Pachimon.Items
{
    public enum ItemTargetAffiliation
    {
        Ally = 0,
        Enemy = 1,
    }

    public enum ItemUseContextKind
    {
        Run = 0,
        Battle = 1,
    }

    public sealed class ItemUseContext
    {
        private ItemUseContext(
            ItemUseContextKind kind,
            ItemTargetAffiliation affiliation,
            PachimonInstance runTarget,
            BattleUnitState battleTarget,
            int effectiveMaxHp,
            int effectiveMaxMn,
            BattleState battleState,
            Func<EffectivePachimonStats> recalculateRunStats)
        {
            Kind = kind;
            Affiliation = affiliation;
            RunTarget = runTarget;
            BattleTarget = battleTarget;
            EffectiveMaxHp = effectiveMaxHp;
            EffectiveMaxMn = effectiveMaxMn;
            BattleState = battleState;
            _recalculateRunStats = recalculateRunStats;
        }

        private readonly Func<EffectivePachimonStats> _recalculateRunStats;

        public ItemUseContextKind Kind { get; }
        public ItemTargetAffiliation Affiliation { get; }
        public PachimonInstance RunTarget { get; }
        public BattleUnitState BattleTarget { get; }
        public int EffectiveMaxHp { get; }
        public int EffectiveMaxMn { get; }
        public BattleState BattleState { get; }
        public string TargetInstanceId =>
            Kind == ItemUseContextKind.Run
                ? RunTarget.InstanceId
                : BattleTarget.InstanceId;
        public int CurrentHp =>
            Kind == ItemUseContextKind.Run
                ? RunTarget.CurrentHp
                : BattleTarget.CurrentHp;
        public int CurrentMn =>
            Kind == ItemUseContextKind.Run
                ? RunTarget.CurrentMn
                : BattleTarget.CurrentMn;

        public static ItemUseContext ForRun(
            PachimonInstance target,
            int effectiveMaxHp,
            ItemTargetAffiliation affiliation)
        {
            return ForRun(target, effectiveMaxHp, target?.MaxMn ?? 0, affiliation);
        }

        public static ItemUseContext ForRun(
            PachimonInstance target,
            int effectiveMaxHp,
            int effectiveMaxMn,
            ItemTargetAffiliation affiliation,
            Func<EffectivePachimonStats> recalculateStats = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (effectiveMaxHp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(effectiveMaxHp));
            }
            if (effectiveMaxMn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(effectiveMaxMn));
            }

            return new ItemUseContext(
                ItemUseContextKind.Run,
                affiliation,
                target,
                null,
                effectiveMaxHp,
                effectiveMaxMn,
                null,
                recalculateStats);
        }

        public static ItemUseContext ForBattle(
            BattleUnitState target,
            ItemTargetAffiliation affiliation,
            PachimonInstance runTarget = null,
            BattleState battleState = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (runTarget != null
                && !string.Equals(
                    runTarget.InstanceId,
                    target.InstanceId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Run and Battle targets must represent the same Pachimon.",
                    nameof(runTarget));
            }

            return new ItemUseContext(
                ItemUseContextKind.Battle,
                affiliation,
                runTarget,
                target,
                target.MaxHp,
                target.MaxMn,
                battleState,
                null);
        }

        public int RestoreHp(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var previousHp = CurrentHp;
            if (Kind == ItemUseContextKind.Run)
            {
                RunTarget.RestoreHp(amount, EffectiveMaxHp);
            }
            else
            {
                if (BattleState != null)
                {
                    BattleState.SupportEffects.RestoreHp(
                        BattleTarget,
                        BattleTarget,
                        amount,
                        applySustainPower: false);
                }
                else
                {
                    BattleTarget.RestoreHp(amount);
                }
            }

            return CurrentHp - previousHp;
        }

        public int RestoreMn(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var previousMn = CurrentMn;
            if (Kind == ItemUseContextKind.Run)
            {
                RunTarget.RestoreMn(amount, EffectiveMaxMn);
            }
            else
            {
                BattleTarget.RestoreMn(amount);
            }

            return CurrentMn - previousMn;
        }

        public int ApplyDamage(int amount, int originId = 1)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var previousHp = CurrentHp;
            if (Kind == ItemUseContextKind.Run)
            {
                RunTarget.ApplyDamage(amount);
            }
            else
            {
                if (BattleState != null)
                {
                    return BattleTrueDamageService.Apply(
                        BattleState,
                        BattleTarget,
                        BattleTarget,
                        new TrueDamageContext(
                            DamageOriginKind.Item,
                            originId,
                            amount,
                            isAttack: false)).AppliedDamage;
                }

                BattleTarget.ApplyDamage(amount);
            }

            return previousHp - CurrentHp;
        }

        public void ApplyPermanentStatChanges(
            IReadOnlyList<GeneratedStatChange> changes,
            string sourceId,
            string displayName)
        {
            if (changes == null || changes.Count == 0)
            {
                throw new ArgumentException(
                    "At least one generated Stat change is required.",
                    nameof(changes));
            }
            if (RunTarget == null)
            {
                throw new InvalidOperationException(
                    "Permanent Stat changes require a Run Pachimon target.");
            }

            foreach (var change in changes)
            {
                RunTarget.AddPermanentStatModifier(
                    change.StatType,
                    change.Amount,
                    sourceId,
                    displayName);
                BattleTarget?.AddPermanentItemStatModifier(
                    change.StatType,
                    change.Amount,
                    sourceId,
                    displayName);
            }

            if (Kind == ItemUseContextKind.Run)
            {
                var hpDelta = changes
                    .Where(change => change.StatType == PachimonStatType.MaxHp)
                    .Sum(change => change.Amount);
                var mnDelta = changes
                    .Where(change => change.StatType == PachimonStatType.MaxMn)
                    .Sum(change => change.Amount);
                var recalculated = _recalculateRunStats?.Invoke();
                var newMaxHp = recalculated?.MaxHp
                    ?? Math.Max(0, EffectiveMaxHp + hpDelta);
                var newMaxMn = recalculated?.MaxMn
                    ?? Math.Max(0, EffectiveMaxMn + mnDelta);
                RunTarget.ApplyEffectiveMaxHpChange(
                    EffectiveMaxHp,
                    newMaxHp);
                RunTarget.ApplyEffectiveMaxMnChange(
                    EffectiveMaxMn,
                    newMaxMn);
            }
        }
    }
}
