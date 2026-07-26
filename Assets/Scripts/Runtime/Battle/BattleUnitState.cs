using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattleUnitState
    {
        private readonly PachimonSkillSlot[] _skillSlots;
        private readonly int[] _skillIds;
        private readonly int[] _passiveIds;
        private readonly Dictionary<int, long> _cooldownReadyTicks = new();

        public BattleUnitState(
            string instanceId,
            int speciesId,
            string displayName,
            BattleSide side,
            int slotIndex,
            EffectivePachimonStats startingStats,
            int currentHp,
            int currentMn,
            IEnumerable<PachimonSkillSlot> skillSlots,
            IEnumerable<int> passiveIds)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("Instance ID is required.", nameof(instanceId));
            }

            if (speciesId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(speciesId));
            }

            if (slotIndex < 0 || slotIndex >= BattleSideState.PartySize)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            InstanceId = instanceId;
            SpeciesId = speciesId;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? $"Pachimon #{speciesId}"
                : displayName;
            Side = side;
            SlotIndex = slotIndex;
            StartingStats = startingStats
                ?? throw new ArgumentNullException(nameof(startingStats));
            CurrentHp = Math.Max(0, Math.Min(currentHp, StartingStats.MaxHp));
            CurrentMn = Math.Max(0, Math.Min(currentMn, StartingStats.MaxMn));
            _skillSlots = skillSlots?.ToArray()
                ?? throw new ArgumentNullException(nameof(skillSlots));
            if (_skillSlots.Length == 0
                || _skillSlots.Any(slot => slot == null)
                || _skillSlots.Select(slot => slot.SlotId).Distinct().Count() != _skillSlots.Length)
            {
                throw new ArgumentException(
                    "A Battle Unit requires valid, uniquely identified Skill Slots.",
                    nameof(skillSlots));
            }

            _skillIds = _skillSlots.Select(slot => slot.SkillId).ToArray();
            _passiveIds = passiveIds?.Distinct().ToArray()
                ?? throw new ArgumentNullException(nameof(passiveIds));
            if (_passiveIds.Length == 0 || _passiveIds.Any(passiveId => passiveId <= 0))
            {
                throw new ArgumentException(
                    "A Battle Unit requires at least one valid Passive ID.",
                    nameof(passiveIds));
            }
        }

        public string InstanceId { get; }
        public int SpeciesId { get; }
        public string DisplayName { get; }
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public EffectivePachimonStats StartingStats { get; }
        public int CurrentHp { get; private set; }
        public int MaxHp => StartingStats.MaxHp;
        public int CurrentMn { get; private set; }
        public int MaxMn => StartingStats.MaxMn;
        public IReadOnlyList<PachimonSkillSlot> SkillSlots => _skillSlots;
        public IReadOnlyList<int> SkillIds => _skillIds;
        public IReadOnlyList<int> PassiveIds => _passiveIds;
        public long NextTurnTick { get; internal set; }
        public int TiePriority { get; internal set; }
        public bool IsAlive => CurrentHp > 0;
        public bool IsDefeated => !IsAlive;

        public PachimonSkillSlot GetSkillSlot(int slotId)
        {
            ValidateSkillSlotId(slotId);
            return _skillSlots.FirstOrDefault(slot => slot.SlotId == slotId);
        }

        public long GetCooldownReadyTick(int slotId)
        {
            ValidateSkillSlotId(slotId);
            return _cooldownReadyTicks.TryGetValue(slotId, out var readyTick)
                ? readyTick
                : 0L;
        }

        public bool IsSkillReady(int slotId, long currentTick)
        {
            if (currentTick < 0) throw new ArgumentOutOfRangeException(nameof(currentTick));
            return currentTick >= GetCooldownReadyTick(slotId);
        }

        internal void SetCooldownReadyTick(int slotId, long readyTick)
        {
            ValidateSkillSlotId(slotId);
            if (readyTick < 0) throw new ArgumentOutOfRangeException(nameof(readyTick));
            _cooldownReadyTicks[slotId] = readyTick;
        }

        public int ApplyDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var appliedDamage = Math.Min(CurrentHp, amount);
            CurrentHp -= appliedDamage;
            return appliedDamage;
        }

        public int RestoreHp(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var restoredHp = Math.Min(MaxHp - CurrentHp, amount);
            CurrentHp += restoredHp;
            return restoredHp;
        }

        public bool CanSpendMn(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            return CurrentMn >= amount;
        }

        public bool TrySpendMn(int amount)
        {
            if (!CanSpendMn(amount)) return false;
            CurrentMn -= amount;
            return true;
        }

        public int RestoreMn(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var restoredMn = Math.Min(MaxMn - CurrentMn, amount);
            CurrentMn += restoredMn;
            return restoredMn;
        }

        private static void ValidateSkillSlotId(int slotId)
        {
            if (slotId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotId));
            }
        }
    }
}
