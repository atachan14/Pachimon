using System;
using System.Collections.Generic;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public enum BattleStatusId
    {
        Leak = 1,
        StoredCharge = 2,
        Stun = 100,
        Slow = 101,
        Paralysis = 102,
        Chill = 103,
        Freeze = 104,
        Knockout = 105,
        Charge = 106,
        Toxin = 108,
        ToxinGrowth = 109,
        FireGrowth = 110,
        AddChain = 111,
        Burn = 112,
        ComboMasterBonus = 113,
        IceGrowth = 114,
        FrozenBreakSelf = 115,
        LaunchCeremony = 116,
        LeafGrowth = 117,
        Flying = 118,
        WindErosion = 119,
        HealingWind = 120,
        StillAir = 121,
        OneTwo = 122,
        DragonBoxer = 123,
        Footwork = 124,
        SweetScience = 125,
        DragonDance = 126,
        DragonCranker = 127,
        DragonDefense = 128,
        Weakness = 129,
        WeaklingBullySpeed = 130,
        BurningFlowerLeaf = 131,
        BurningFlowerFire = 132,
        ElectricShield = 133,
        PoisonMagicianGrowth = 134,
        WindRiderGrowth = 135,
        WindMagicianGrowth = 136,
        Charm = 137,
        Intangible = 138,
        Clone = 139,
        WindGod = 140,
        DragonInstall = 141,
        Pollen = 142,
    }

    [Flags]
    public enum BattleStatusCategory
    {
        None = 0,
        Leak = 1 << 0,
        Charge = 1 << 2,
        Stun = 1 << 3,
        Slow = 1 << 4,
        Toxin = 1 << 5,
        Burn = 1 << 6,
        Untargetable = 1 << 7,
    }

    public sealed class ToxinApplicationRecord
    {
        public ToxinApplicationRecord(
            string sourceInstanceId,
            string sourceDisplayName,
            int appliedValue)
        {
            SourceInstanceId = sourceInstanceId ?? string.Empty;
            SourceDisplayName = sourceDisplayName ?? string.Empty;
            AppliedValue = appliedValue;
        }

        public string SourceInstanceId { get; }
        public string SourceDisplayName { get; }
        public int AppliedValue { get; }
    }

    public readonly struct ToxinTickResult
    {
        public ToxinTickResult(int damage, int decay)
        {
            Damage = damage;
            Decay = decay;
        }

        public int Damage { get; }
        public int Decay { get; }
    }

    public sealed class BattleStatusInstance
    {
        public BattleStatusInstance(
            BattleStatusId statusId,
            BattleStatusCategory categories,
            BattleUnitState source,
            int value,
            int stackCount = 1,
            int? durationTicks = null,
            object runtimeData = null,
            BattleStatusAsset definition = null)
        {
            if (source == null && statusId != BattleStatusId.Toxin)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (stackCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stackCount));
            }
            if (durationTicks.HasValue && durationTicks.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTicks));
            }

            StatusId = statusId;
            Categories = categories;
            Source = source;
            Value = value;
            StackCount = stackCount;
            RemainingTicks = durationTicks;
            RuntimeData = runtimeData;
            Definition = definition;
        }

        public BattleStatusId StatusId { get; }
        public BattleStatusCategory Categories { get; }
        public BattleUnitState Source { get; }
        public int Value { get; private set; }
        public int StackCount { get; }
        public int? RemainingTicks { get; private set; }
        public object RuntimeData { get; }
        public BattleStatusAsset Definition { get; }
        public decimal DamageWork { get; private set; }
        public decimal DecayWork { get; private set; }
        public IReadOnlyList<ToxinApplicationRecord> ToxinApplications =>
            _toxinApplications;
        public bool IsTimed => RemainingTicks.HasValue;
        public bool IsVisible => true;
        public bool IsExpired =>
            RemainingTicks == 0
            || (StatusId == BattleStatusId.Freeze && Value <= 0)
            || (StatusId == BattleStatusId.WindErosion && Value <= 0)
            || (StatusId == BattleStatusId.Pollen && Value <= 0)
            || (((Categories
                    & (BattleStatusCategory.Slow | BattleStatusCategory.Toxin)) != 0)
                && Value <= 0);

        public void AddRemainingTicks(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (ticks == 0 || !RemainingTicks.HasValue)
                return;
            RemainingTicks = checked(RemainingTicks.Value + ticks);
        }

        private readonly List<ToxinApplicationRecord> _toxinApplications = new();

        public string DisplayName => Definition != null
            ? Definition.GetDisplayName(this)
            : GetLegacyDisplayName();

        public string Description => Definition?.GetDescription(this)
            ?? string.Empty;

        private string GetLegacyDisplayName() => StatusId switch
        {
            BattleStatusId.Leak => $"漏電 {Value}",
            BattleStatusId.StoredCharge => StackCount > 1
                ? $"蓄電 ×{StackCount}"
                : "蓄電",
            BattleStatusId.Stun =>
                FormatTimedName(Definition?.DisplayName ?? "Stun"),
            BattleStatusId.Slow =>
                FormatTimedName($"{Definition?.DisplayName ?? "Slow"} {Value}"),
            BattleStatusId.Paralysis =>
                FormatTimedName(
                    $"{Definition?.DisplayName ?? "Paralysis"} {Value}"),
            BattleStatusId.Chill =>
                FormatTimedName($"{Definition?.DisplayName ?? "Chill"} {Value}"),
            BattleStatusId.Freeze => FormatTimedName("Freeze"),
            BattleStatusId.Knockout => FormatTimedName("Knockout"),
            BattleStatusId.Toxin =>
                $"{Definition?.DisplayName ?? "毒素"} {Value}",
            BattleStatusId.ToxinGrowth =>
                $"毒素適応 +{Value * StackCount}%",
            BattleStatusId.Pollen => $"{Definition?.DisplayName ?? "花粉"} {Value}",
            BattleStatusId.FireGrowth =>
                $"燃える男 Fire +{Value * StackCount}",
            BattleStatusId.AddChain =>
                $"アドチェイン {AddChainRuntime.FormatUnits(Value)}",
            BattleStatusId.Burn => $"{Definition?.DisplayName ?? "火傷"} {Value}",
            BattleStatusId.ComboMasterBonus =>
                $"コンボマスター DB +{Value * StackCount}",
            BattleStatusId.IceGrowth =>
                $"氷の刃 Ice +{Value * StackCount}",
            BattleStatusId.LeafGrowth =>
                $"粉植物 Leaf +{Value * StackCount}",
            BattleStatusId.BurningFlowerLeaf =>
                $"{Definition?.DisplayName ?? "燃える花・草"} +{Value * StackCount}",
            BattleStatusId.BurningFlowerFire =>
                $"{Definition?.DisplayName ?? "燃える花・炎"} +{Value * StackCount}",
            _ => StatusId.ToString(),
        };

        private string FormatTimedName(string name)
        {
            return RemainingTicks.HasValue
                ? $"{name} [{RemainingTicks.Value}]"
                : name;
        }

        internal void Advance(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (!RemainingTicks.HasValue || ticks == 0)
            {
                return;
            }

            RemainingTicks = Math.Max(0, RemainingTicks.Value - ticks);
        }

        internal void AddDuration(int ticks)
        {
            if (ticks <= 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (!RemainingTicks.HasValue)
            {
                return;
            }

            RemainingTicks = checked(RemainingTicks.Value + ticks);
        }

        internal void AddValue(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Value = checked(Value + amount);
        }

        internal void DecayValue(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Value = Math.Max(0, Value - amount);
        }

        internal void AccumulateValueDecay(decimal amount)
        {
            if (amount < 0m) throw new ArgumentOutOfRangeException(nameof(amount));
            DecayWork += amount;
            var decay = Math.Min(Value, SignedStatMath.FloorNonNegative(DecayWork));
            DecayWork -= decay;
            Value -= decay;
        }

        public int GetSpeedReduction(int valueOverride = -1)
        {
            var value = valueOverride >= 0 ? valueOverride : Value;
            var totalValue = checked(value * StackCount);
            return Definition is SlowStatusAsset slow
                ? slow.CalculateSpeedReduction(totalValue)
                : totalValue;
        }

        internal void AddToxinApplication(
            BattleUnitState source,
            int appliedValue)
        {
            if (StatusId != BattleStatusId.Toxin)
            {
                throw new InvalidOperationException(
                    "Only Toxin can store Toxin application history.");
            }

            if (source == null) throw new ArgumentNullException(nameof(source));
            if (appliedValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(appliedValue));
            }

            AddToxinApplication(new ToxinApplicationRecord(
                source.InstanceId,
                source.DisplayName,
                appliedValue));
        }

        internal void AddToxinApplication(ToxinApplicationRecord application)
        {
            if (StatusId != BattleStatusId.Toxin)
            {
                throw new InvalidOperationException(
                    "Only Toxin can store Toxin application history.");
            }

            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (application.AppliedValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(application));
            }

            Value = checked(Value + application.AppliedValue);
            _toxinApplications.Add(application);
        }

        internal ToxinTickResult AccumulateToxinTick(
            decimal unroundedDamage,
            int decayPerTick)
        {
            if (StatusId != BattleStatusId.Toxin)
            {
                throw new InvalidOperationException(
                    "Only Toxin can accumulate Toxin tick work.");
            }

            if (unroundedDamage < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(unroundedDamage));
            }

            if (decayPerTick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(decayPerTick));
            }

            DamageWork += unroundedDamage;
            var damage = SignedStatMath.FloorNonNegative(DamageWork);
            var decay = Math.Min(Value, decayPerTick);
            DamageWork -= damage;
            Value -= decay;
            return new ToxinTickResult(damage, decay);
        }

        internal void CopyToxinRuntimeFrom(BattleStatusInstance original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            DamageWork = original.DamageWork;
            DecayWork = original.DecayWork;
            _toxinApplications.Clear();
            _toxinApplications.AddRange(original._toxinApplications);
        }
    }
}
