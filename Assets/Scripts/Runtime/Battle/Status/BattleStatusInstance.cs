using System;

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
        Charging = 106,
        Charged = 107,
    }

    [Flags]
    public enum BattleStatusCategory
    {
        None = 0,
        Leak = 1 << 0,
        WeatherGranted = 1 << 1,
        Charge = 1 << 2,
        Stun = 1 << 3,
        Slow = 1 << 4,
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
            object tuning = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
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
            Tuning = tuning;
        }

        public BattleStatusId StatusId { get; }
        public BattleStatusCategory Categories { get; }
        public BattleUnitState Source { get; }
        public int Value { get; private set; }
        public int StackCount { get; }
        public int? RemainingTicks { get; private set; }
        public object Tuning { get; }
        public bool IsTimed => RemainingTicks.HasValue;
        public bool IsExpired =>
            RemainingTicks == 0
            || ((Categories & BattleStatusCategory.Slow) != 0 && Value <= 0);

        public string DisplayName => StatusId switch
        {
            BattleStatusId.Leak => "漏電",
            BattleStatusId.StoredCharge => StackCount > 1
                ? $"蓄電 ×{StackCount}"
                : "蓄電",
            BattleStatusId.Stun => FormatTimedName("Stun"),
            BattleStatusId.Slow => FormatTimedName($"Slow {Value}"),
            BattleStatusId.Paralysis =>
                FormatTimedName($"Paralysis {Value}"),
            BattleStatusId.Chill => FormatTimedName($"Chill {Value}"),
            BattleStatusId.Freeze => FormatTimedName("Freeze"),
            BattleStatusId.Knockout => FormatTimedName("Knockout"),
            BattleStatusId.Charging => FormatTimedName($"充電中 {Value}"),
            BattleStatusId.Charged => FormatTimedName($"充電完了 {Value}"),
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
    }
}
