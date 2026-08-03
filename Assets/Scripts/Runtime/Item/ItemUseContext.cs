using System;
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
            int effectiveMaxHp)
        {
            Kind = kind;
            Affiliation = affiliation;
            RunTarget = runTarget;
            BattleTarget = battleTarget;
            EffectiveMaxHp = effectiveMaxHp;
        }

        public ItemUseContextKind Kind { get; }
        public ItemTargetAffiliation Affiliation { get; }
        public PachimonInstance RunTarget { get; }
        public BattleUnitState BattleTarget { get; }
        public int EffectiveMaxHp { get; }
        public string TargetInstanceId =>
            Kind == ItemUseContextKind.Run
                ? RunTarget.InstanceId
                : BattleTarget.InstanceId;
        public int CurrentHp =>
            Kind == ItemUseContextKind.Run
                ? RunTarget.CurrentHp
                : BattleTarget.CurrentHp;

        public static ItemUseContext ForRun(
            PachimonInstance target,
            int effectiveMaxHp,
            ItemTargetAffiliation affiliation)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (effectiveMaxHp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(effectiveMaxHp));
            }

            return new ItemUseContext(
                ItemUseContextKind.Run,
                affiliation,
                target,
                null,
                effectiveMaxHp);
        }

        public static ItemUseContext ForBattle(
            BattleUnitState target,
            ItemTargetAffiliation affiliation,
            PachimonInstance runTarget = null)
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
                target.MaxHp);
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
                BattleTarget.RestoreHp(amount);
            }

            return CurrentHp - previousHp;
        }

        public int ApplyDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var previousHp = CurrentHp;
            if (Kind == ItemUseContextKind.Run)
            {
                RunTarget.ApplyDamage(amount);
            }
            else
            {
                BattleTarget.ApplyDamage(amount);
            }

            return previousHp - CurrentHp;
        }
    }
}
