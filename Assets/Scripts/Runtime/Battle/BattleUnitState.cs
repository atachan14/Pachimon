using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattleUnitState
    {
        private readonly List<PachimonSkillSlot> _skillSlots;
        private readonly List<int> _skillIds;
        private readonly int[] _passiveIds;
        private readonly Dictionary<int, BattleSkillCooldownState> _cooldowns = new();
        private readonly HashSet<int> _oncePerBattleUsedSkillSlots = new();
        private readonly List<BattleStatusInstance> _statuses = new();
        private readonly List<BattleShieldInstance> _shields = new();
        private readonly List<IStatModifier> _permanentItemModifiers = new();
        private readonly PachimonStats _battleBaseStats;
        private EffectivePachimonStats _battleStats;
        private Func<IEnumerable<IStatModifier>> _battleModifierProvider;
        private bool _battleStatsDirty = true;
        private long _nextShieldApplicationOrder;

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
            _battleBaseStats = CreateBattleBaseStats(startingStats);
            CurrentHp = Math.Max(0, Math.Min(currentHp, StartingStats.MaxHp));
            CurrentMn = Math.Max(0, Math.Min(currentMn, StartingStats.MaxMn));
            _skillSlots = skillSlots?.ToList()
                ?? throw new ArgumentNullException(nameof(skillSlots));
            if (_skillSlots.Count == 0
                || _skillSlots.Any(slot => slot == null)
                || _skillSlots.Select(slot => slot.SlotId).Distinct().Count() != _skillSlots.Count)
            {
                throw new ArgumentException(
                    "A Battle Unit requires valid, uniquely identified Skill Slots.",
                    nameof(skillSlots));
            }

            _skillIds = _skillSlots.Select(slot => slot.SkillId).ToList();
            _passiveIds = passiveIds?.Distinct().ToArray()
                ?? throw new ArgumentNullException(nameof(passiveIds));
            if (_passiveIds.Length == 0 || _passiveIds.Any(passiveId => passiveId <= 0))
            {
                throw new ArgumentException(
                    "A Battle Unit requires at least one valid Passive ID.",
                    nameof(passiveIds));
            }

            Timing = new BattleUnitTimingState();
        }

        public string InstanceId { get; }
        public int SpeciesId { get; }
        public string DisplayName { get; }
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public EffectivePachimonStats StartingStats { get; }
        public int CurrentHp { get; private set; }
        public int MaxHp => GetBattleStats().MaxHp;
        public int CurrentMn { get; private set; }
        public int MaxMn => GetBattleStats().MaxMn;
        public IReadOnlyList<PachimonSkillSlot> SkillSlots => _skillSlots;
        public IReadOnlyList<int> SkillIds => _skillIds;
        public IReadOnlyList<int> PassiveIds => _passiveIds;
        public IReadOnlyList<BattleStatusInstance> Statuses => _statuses;
        public IReadOnlyList<BattleShieldInstance> Shields => _shields;
        public int TotalShield => _shields.Sum(shield => shield.Value);
        public BattleUnitTimingState Timing { get; }
        public int TiePriority { get; internal set; }
        public bool IsAlive => CurrentHp > 0;
        public bool IsDefeated => !IsAlive;
        public bool CanAddSkill =>
            _skillSlots.Count < PachimonInstance.MaxSkillSlots;

        public PachimonSkillSlot GetSkillSlot(int slotId)
        {
            ValidateSkillSlotId(slotId);
            return _skillSlots.FirstOrDefault(slot => slot.SlotId == slotId);
        }

        public int GetCooldownRemainingTicks(int slotId)
        {
            ValidateSkillSlotId(slotId);
            return _cooldowns.TryGetValue(slotId, out var cooldown)
                ? BattleTickMath.GetTicksToComplete(
                    cooldown.RemainingWork,
                    GetBattleStatValue(PachimonStatType.Haste))
                : 0;
        }

        public BattleSkillCooldownState GetCooldown(int slotId)
        {
            ValidateSkillSlotId(slotId);
            return _cooldowns.TryGetValue(slotId, out var cooldown)
                ? cooldown
                : null;
        }

        public bool IsSkillReady(int slotId)
        {
            return GetCooldownRemainingTicks(slotId) == 0;
        }

        public bool HasUsedOncePerBattleSkill(int slotId)
        {
            ValidateSkillSlotId(slotId);
            return _oncePerBattleUsedSkillSlots.Contains(slotId);
        }

        public bool TryUseOncePerBattleSkill(int slotId)
        {
            ValidateSkillSlotId(slotId);
            return _oncePerBattleUsedSkillSlots.Add(slotId);
        }

        public bool AddSkill(int skillId)
        {
            if (skillId <= 0 || !CanAddSkill)
            {
                return false;
            }

            var nextSlotId = _skillSlots.Max(slot => slot.SlotId) + 1;
            _skillSlots.Add(new PachimonSkillSlot(nextSlotId, skillId));
            _skillIds.Add(skillId);
            return true;
        }

        public void SetActionClockPaused(bool isPaused)
        {
            Timing.SetPaused(isPaused);
        }

        internal void StartCooldown(int slotId, decimal totalWork)
        {
            ValidateSkillSlotId(slotId);
            if (totalWork < 0m) throw new ArgumentOutOfRangeException(nameof(totalWork));
            _cooldowns[slotId] = new BattleSkillCooldownState(totalWork, totalWork);
        }

        internal void ClearCooldown(int slotId)
        {
            ValidateSkillSlotId(slotId);
            _cooldowns.Remove(slotId);
        }

        internal void AdvanceClocksOneTick()
        {
            if (!IsAlive)
            {
                Timing.MarkDefeated();
                return;
            }

            if (Timing.IsPaused)
            {
                return;
            }

            Timing.Advance(BattleTickMath.GetProgressPerTick(
                GetBattleStatValue(PachimonStatType.Speed)));
            foreach (var cooldown in _cooldowns.Values)
            {
                cooldown.Advance(BattleTickMath.GetProgressPerTick(
                    GetBattleStatValue(PachimonStatType.Haste)));
            }
        }

        public int GetActionRemainingTicks()
        {
            if (Timing.IsComplete)
            {
                return 0;
            }

            if (Timing.IsPaused)
            {
                return int.MaxValue;
            }

            var remainingWork = Timing.RemainingWork;
            var slowValues = _statuses
                .Where(status =>
                    (status.Categories & BattleStatusCategory.Slow) != 0)
                .Select(status => checked(status.Value * status.StackCount))
                .ToArray();
            var currentSlow = slowValues.Sum();
            var baseSpeed = checked(
                GetBattleStatValue(PachimonStatType.Speed) + currentSlow);
            if (slowValues.Length == 0)
            {
                return BattleTickMath.GetTicksToComplete(
                    remainingWork,
                    baseSpeed);
            }

            var elapsedTicks = 0;
            while (remainingWork > 0m)
            {
                var slow = slowValues.Sum(value =>
                    Math.Max(0, value - elapsedTicks));
                var speed = checked(baseSpeed - slow);
                remainingWork -= BattleTickMath.GetProgressPerTick(speed);
                elapsedTicks++;
                if (elapsedTicks == int.MaxValue)
                {
                    throw new OverflowException(
                        "Action timing prediction exceeded the Int32 tick range.");
                }
            }

            return elapsedTicks;
        }

        internal BattleUnitState CreateSimulationClone()
        {
            var clone = new BattleUnitState(
                InstanceId,
                SpeciesId,
                DisplayName,
                Side,
                SlotIndex,
                StartingStats,
                CurrentHp,
                CurrentMn,
                _skillSlots,
                _passiveIds);
            foreach (var cooldown in _cooldowns)
            {
                clone._cooldowns[cooldown.Key] =
                    cooldown.Value.CreateSimulationClone();
            }
            clone._oncePerBattleUsedSkillSlots.UnionWith(
                _oncePerBattleUsedSkillSlots);
            clone._permanentItemModifiers.AddRange(_permanentItemModifiers);
            clone.InvalidateBattleStats();

            clone.Timing.CopyFrom(Timing);
            clone.TiePriority = TiePriority;
            return clone;
        }

        internal void CopyStatusesForSimulation(
            BattleUnitState original,
            IReadOnlyDictionary<BattleUnitState, BattleUnitState> unitMap)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (unitMap == null) throw new ArgumentNullException(nameof(unitMap));
            Timing.CopyFrom(original.Timing);
            TiePriority = original.TiePriority;
            _statuses.Clear();
            foreach (var status in original.Statuses)
            {
                BattleUnitState sourceClone = null;
                if (status.Source != null
                    && !unitMap.TryGetValue(status.Source, out sourceClone))
                {
                    throw new InvalidOperationException(
                        "A Status source does not belong to the Battle.");
                }

                var runtimeData = status.RuntimeData is FrozenBreakRuntimeState frozenBreak
                    ? frozenBreak.CreateSimulationClone()
                    : status.RuntimeData;
                var cloneStatus = new BattleStatusInstance(
                    status.StatusId,
                    status.Categories,
                    sourceClone,
                    status.Value,
                    status.StackCount,
                    status.RemainingTicks,
                    runtimeData,
                    status.Definition);
                cloneStatus.CopyToxinRuntimeFrom(status);
                _statuses.Add(cloneStatus);
            }

            _shields.Clear();
            _shields.AddRange(original.Shields.Select(
                shield => shield.CreateSimulationClone()));
            _nextShieldApplicationOrder = original._nextShieldApplicationOrder;
        }

        public BattleShieldInstance AddShield(
            int value,
            int? durationTicks = null)
        {
            var shield = new BattleShieldInstance(
                _nextShieldApplicationOrder++,
                value,
                durationTicks);
            _shields.Add(shield);
            return shield;
        }

        public BattleShieldAbsorptionResult AbsorbDamage(int damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            var remaining = damage;
            var absorbed = 0;
            foreach (var shield in _shields
                         .OrderBy(current => current.RemainingTicks ?? int.MaxValue)
                         .ThenBy(current => current.ApplicationOrder)
                         .ToArray())
            {
                var currentAbsorbed = shield.Absorb(remaining);
                absorbed = checked(absorbed + currentAbsorbed);
                remaining -= currentAbsorbed;
                if (remaining == 0)
                {
                    break;
                }
            }

            _shields.RemoveAll(shield => shield.IsExpired);
            return new BattleShieldAbsorptionResult(damage, absorbed);
        }

        public int RemoveAllShields()
        {
            var removedValue = TotalShield;
            _shields.Clear();
            return removedValue;
        }

        internal void AdvanceShields(int ticks)
        {
            foreach (var shield in _shields)
            {
                shield.Advance(ticks);
            }
            _shields.RemoveAll(shield => shield.IsExpired);
        }

        public int ApplyDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var appliedDamage = Math.Min(CurrentHp, amount);
            CurrentHp -= appliedDamage;
            if (CurrentHp == 0 && _statuses.Count > 0)
            {
                _statuses.Clear();
                InvalidateBattleStats();
            }
            if (CurrentHp == 0)
            {
                _shields.Clear();
            }
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

        public void AddPermanentItemStatModifier(
            PachimonStatType statType,
            int amount,
            string sourceId,
            string displayName)
        {
            var oldMaxHp = MaxHp;
            var oldMaxMn = MaxMn;
            _permanentItemModifiers.Add(new FixedStatModifier(
                statType,
                StatModifierOperation.DirectAdditive,
                amount,
                new StatModifierSource(
                    StatModifierSourceType.Item,
                    sourceId,
                    displayName)));
            InvalidateBattleStats();

            if (statType == PachimonStatType.MaxHp)
            {
                CurrentHp = Math.Clamp(
                    CurrentHp + Math.Max(0, MaxHp - oldMaxHp),
                    0,
                    MaxHp);
            }
            else if (statType == PachimonStatType.MaxMn)
            {
                CurrentMn = Math.Clamp(
                    CurrentMn + Math.Max(0, MaxMn - oldMaxMn),
                    0,
                    MaxMn);
            }
        }

        public void ApplyOrReplaceStatus(BattleStatusInstance status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            var existingIndex = _statuses.FindIndex(
                current => current.StatusId == status.StatusId);
            if (existingIndex >= 0)
            {
                _statuses[existingIndex] = status;
                InvalidateBattleStats();
                return;
            }

            _statuses.Add(status);
            InvalidateBattleStats();
        }

        internal void AddStatusInstance(BattleStatusInstance status)
        {
            _statuses.Add(status
                ?? throw new ArgumentNullException(nameof(status)));
            InvalidateBattleStats();
        }

        public BattleStatusInstance GetStatus(BattleStatusId statusId)
        {
            return _statuses.FirstOrDefault(
                current => current.StatusId == statusId);
        }

        public IReadOnlyList<BattleStatusInstance> GetStatuses(
            BattleStatusId statusId)
        {
            return _statuses
                .Where(current => current.StatusId == statusId)
                .ToArray();
        }

        public bool HasStatusCategory(BattleStatusCategory category)
        {
            if (category == BattleStatusCategory.None)
            {
                return false;
            }

            return _statuses.Any(status =>
                (status.Categories & category) != 0);
        }

        public bool IsTargetable =>
            IsAlive
            && !HasStatusCategory(BattleStatusCategory.Untargetable);

        public int GetStatusCategoryValue(BattleStatusCategory category)
        {
            if (category == BattleStatusCategory.None)
            {
                return 0;
            }

            return _statuses
                .Where(status => (status.Categories & category) != 0)
                .Sum(status => checked(status.Value * status.StackCount));
        }

        public int GetBattleStatValue(PachimonStatType statType)
        {
            return GetBattleStats().GetValue(statType);
        }

        public EffectivePachimonStats GetBattleStats()
        {
            if (_battleStatsDirty || _battleStats == null)
            {
                _battleStats = EffectivePachimonStats.Calculate(
                    _battleBaseStats,
                    BattleStatusStatModifierFactory.Create(_statuses)
                        .Concat(_permanentItemModifiers)
                        .Concat(_battleModifierProvider?.Invoke()
                            ?? Enumerable.Empty<IStatModifier>()));
                _battleStatsDirty = false;
            }

            return _battleStats;
        }

        internal void SetBattleModifierProvider(
            Func<IEnumerable<IStatModifier>> provider)
        {
            _battleModifierProvider = provider;
            InvalidateBattleStats();
        }

        internal void NotifyBattleContextChanged()
        {
            InvalidateBattleStats();
        }

        public void AddStatusStacks(
            BattleStatusId statusId,
            BattleStatusCategory categories,
            BattleUnitState source,
            int value,
            int stackCount,
            BattleStatusAsset definition = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (stackCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stackCount));
            }

            var existing = GetStatus(statusId);
            ApplyOrReplaceStatus(new BattleStatusInstance(
                statusId,
                categories,
                source,
                value,
                checked((existing?.StackCount ?? 0) + stackCount),
                definition: definition ?? existing?.Definition));
        }

        public bool TryConsumeStatus(
            BattleStatusId statusId,
            out BattleStatusInstance status)
        {
            var index = _statuses.FindIndex(current => current.StatusId == statusId);
            if (index < 0)
            {
                status = null;
                return false;
            }

            status = _statuses[index];
            _statuses.RemoveAt(index);
            InvalidateBattleStats();
            return true;
        }

        internal bool TryRemoveStatusInstance(BattleStatusInstance status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            var removed = _statuses.Remove(status);
            if (removed)
            {
                InvalidateBattleStats();
            }
            return removed;
        }

        internal IReadOnlyList<BattleStatusInstance> AdvanceStatuses(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            var expired = new List<BattleStatusInstance>();
            var changedAny = false;
            for (var index = _statuses.Count - 1; index >= 0; index--)
            {
                var status = _statuses[index];
                status.Advance(ticks);
                changedAny |= status.IsTimed;
                if ((status.Categories & BattleStatusCategory.Slow) != 0)
                {
                    var decayPerTick = status.Definition is SlowStatusAsset slow
                        ? slow.DecayPerTick
                        : 1;
                    status.DecayValue(checked(ticks * decayPerTick));
                    changedAny = true;
                }
                if (status.StatusId == BattleStatusId.WindErosion)
                {
                    var decayPerTick = status.Definition
                        is WindErosionStatusAsset erosion
                            ? erosion.DecayPerTick
                            : 1;
                    status.DecayValue(checked(ticks * decayPerTick));
                    changedAny = true;
                }
                if (!status.IsExpired)
                {
                    continue;
                }

                _statuses.RemoveAt(index);
                expired.Add(status);
            }

            if (changedAny || expired.Count > 0)
            {
                InvalidateBattleStats();
            }

            return expired;
        }

        private static PachimonStats CreateBattleBaseStats(
            EffectivePachimonStats startingStats)
        {
            var values = new int[(int)PachimonStatType.Count];
            for (var index = 0; index < values.Length; index++)
            {
                values[index] = startingStats.GetValue((PachimonStatType)index);
            }

            return new PachimonStats(
                values,
                resourceDisplayMultiplier: 1,
                specialStatDivisor: 1);
        }

        private void InvalidateBattleStats()
        {
            _battleStatsDirty = true;
        }

        internal void NotifyStatusValueChanged()
        {
            InvalidateBattleStats();
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
