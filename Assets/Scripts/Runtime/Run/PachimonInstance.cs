using System;
using System.Collections.Generic;
using Pachimon.Data;
using Pachimon.Items;

namespace Pachimon.Run
{
    public sealed class PachimonInstance
    {
        public const int MaxSkillSlots = 6;

        private readonly List<int> _skillIds = new();
        private readonly List<PachimonSkillSlot> _skillSlots = new();
        private readonly List<int> _passiveIds = new();
        private readonly List<IStatModifier> _permanentStatModifiers = new();
        private readonly Dictionary<EquipmentSlot, EquippedItem> _equipment = new();
        private readonly List<AppliedEngraving> _engravings = new();
        private int _nextSkillSlotId = 1;

        public PachimonInstance(
            string instanceId,
            int speciesId,
            AllocationType allocationType,
            int fixedSkillId,
            int fixedPassiveId,
            PachimonStats stats,
            PachimonSubStatBindings subStatBindings = null)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("Instance ID is required.", nameof(instanceId));
            }

            if (fixedSkillId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fixedSkillId));
            }

            if (fixedPassiveId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fixedPassiveId));
            }

            InstanceId = instanceId;
            SpeciesId = speciesId;
            AllocationType = allocationType;
            FixedSkillId = fixedSkillId;
            FixedPassiveId = fixedPassiveId;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            SubStatBindings = subStatBindings ?? PachimonSubStatBindings.CreateDefault();
            CurrentHp = Stats.MaxHp;
            CurrentMn = Stats.MaxMn;
            AddSkillSlot(fixedSkillId);
            _passiveIds.Add(fixedPassiveId);
        }

        public string InstanceId { get; }

        public int SpeciesId { get; }

        public AllocationType AllocationType { get; }

        public int FixedSkillId { get; }

        public int FixedPassiveId { get; }

        public PachimonStats Stats { get; }

        public PachimonSubStatBindings SubStatBindings { get; }

        public int CurrentHp { get; private set; }

        public int MaxHp => Stats.MaxHp;

        public int CurrentMn { get; private set; }

        public int MaxMn => Stats.MaxMn;

        public bool IsDefeated => CurrentHp <= 0;

        public IReadOnlyList<int> SkillIds => _skillIds;

        public IReadOnlyList<PachimonSkillSlot> SkillSlots => _skillSlots;

        public IReadOnlyList<int> PassiveIds => _passiveIds;

        public IReadOnlyList<IStatModifier> PermanentStatModifiers =>
            _permanentStatModifiers;

        public IReadOnlyDictionary<EquipmentSlot, EquippedItem> Equipment => _equipment;

        public IReadOnlyList<AppliedEngraving> Engravings => _engravings;

        public bool CanAddSkill => _skillSlots.Count < MaxSkillSlots;

        public bool CanAddSkillId(int skillId)
        {
            return skillId > 0 && (_skillIds.Contains(skillId) || CanAddSkill);
        }

        public bool AddSkill(int skillId)
        {
            if (!CanAddSkillId(skillId))
            {
                return false;
            }

            var existing = _skillSlots.Find(slot => slot.SkillId == skillId);
            if (existing != null)
            {
                existing.Upgrade();
            }
            else
            {
                AddSkillSlot(skillId);
            }
            return true;
        }

        public bool TryForgetSkillSlot(int slotId, out int forgottenSkillId)
        {
            forgottenSkillId = 0;
            var index = _skillSlots.FindIndex(slot => slot.SlotId == slotId);
            if (index < 0)
            {
                return false;
            }

            forgottenSkillId = _skillSlots[index].SkillId;
            _skillSlots.RemoveAt(index);
            _skillIds.RemoveAt(index);
            return true;
        }

        public bool AddPassive(int passiveId)
        {
            if (passiveId <= 0 || _passiveIds.Contains(passiveId))
            {
                return false;
            }

            _passiveIds.Add(passiveId);
            return true;
        }

        public bool CanEquip(EquipmentSlot slot)
        {
            return !_equipment.ContainsKey(slot);
        }

        public bool TryEquip(
            EquipmentItemAsset item,
            GeneratedItemData generatedData,
            string sourceId)
        {
            if (item == null
                || generatedData == null
                || generatedData.EquipmentSlot != item.Slot
                || generatedData.StatChanges.Count == 0
                || string.IsNullOrWhiteSpace(sourceId)
                || !CanEquip(item.Slot))
            {
                return false;
            }

            foreach (var change in generatedData.StatChanges)
            {
                if (PachimonSubStatBindings.IsSubStat(change.StatType))
                {
                    SubStatBindings.AddDerivationRatio(
                        change.StatType,
                        change.Amount);
                }
                else
                {
                    AddPermanentStatModifier(
                        change.StatType,
                        change.Amount,
                        sourceId,
                        item.DisplayName);
                }
            }

            _equipment.Add(
                item.Slot,
                new EquippedItem(item.ItemId, item.DisplayName, generatedData));
            return true;
        }

        public void AddPermanentStatModifier(
            PachimonStatType statType,
            int amount,
            string sourceId,
            string displayName)
        {
            if (statType < 0 || statType >= PachimonStatType.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(statType));
            }
            if (amount == 0) throw new ArgumentOutOfRangeException(nameof(amount));

            _permanentStatModifiers.Add(new FixedStatModifier(
                statType,
                StatModifierOperation.DirectAdditive,
                amount,
                new StatModifierSource(
                    StatModifierSourceType.Item,
                    sourceId,
                    displayName)));
        }

        public void RecordAppliedEngraving(
            int itemId,
            string displayName,
            GeneratedItemData generatedData)
        {
            if (itemId <= 0) throw new ArgumentOutOfRangeException(nameof(itemId));
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "Display name is required.",
                    nameof(displayName));
            }
            if (generatedData == null
                || generatedData.ItemId != itemId
                || generatedData.StatChanges.Count == 0)
            {
                throw new ArgumentException(
                    "Applied Engraving data is invalid.",
                    nameof(generatedData));
            }

            _engravings.Add(new AppliedEngraving(
                itemId,
                displayName,
                generatedData));
        }

        public int SetCurrentHp(int currentHp)
        {
            return SetCurrentHp(currentHp, MaxHp);
        }

        public int SetCurrentHp(int currentHp, int effectiveMaxHp)
        {
            ValidateEffectiveMaxHp(effectiveMaxHp);
            CurrentHp = Math.Clamp(currentHp, 0, effectiveMaxHp);
            return CurrentHp;
        }

        public int ApplyDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            CurrentHp = amount >= CurrentHp ? 0 : CurrentHp - amount;
            return CurrentHp;
        }

        public int RestoreHp(int amount)
        {
            return RestoreHp(amount, MaxHp);
        }

        public int RestoreHp(int amount, int effectiveMaxHp)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            ValidateEffectiveMaxHp(effectiveMaxHp);
            var clampedCurrentHp = Math.Min(CurrentHp, effectiveMaxHp);
            var missingHp = effectiveMaxHp - clampedCurrentHp;
            CurrentHp = amount >= missingHp ? effectiveMaxHp : clampedCurrentHp + amount;
            return CurrentHp;
        }

        public int ApplyEffectiveMaxHpChange(int oldEffectiveMaxHp, int newEffectiveMaxHp)
        {
            ValidateEffectiveMaximum(oldEffectiveMaxHp, nameof(oldEffectiveMaxHp));
            ValidateEffectiveMaximum(newEffectiveMaxHp, nameof(newEffectiveMaxHp));

            var increase = Math.Max(0, newEffectiveMaxHp - oldEffectiveMaxHp);
            var adjustedHp = Math.Min((long)CurrentHp + increase, newEffectiveMaxHp);
            CurrentHp = (int)Math.Max(0, adjustedHp);
            return CurrentHp;
        }

        public int SetCurrentMn(int currentMn)
        {
            return SetCurrentMn(currentMn, MaxMn);
        }

        public int SetCurrentMn(int currentMn, int effectiveMaxMn)
        {
            ValidateEffectiveMaximum(effectiveMaxMn, nameof(effectiveMaxMn));
            CurrentMn = Math.Clamp(currentMn, 0, effectiveMaxMn);
            return CurrentMn;
        }

        public int RestoreMn(int amount)
        {
            return RestoreMn(amount, MaxMn);
        }

        public int RestoreMn(int amount, int effectiveMaxMn)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            ValidateEffectiveMaximum(effectiveMaxMn, nameof(effectiveMaxMn));
            var clampedCurrentMn = Math.Min(CurrentMn, effectiveMaxMn);
            var missingMn = effectiveMaxMn - clampedCurrentMn;
            CurrentMn = amount >= missingMn ? effectiveMaxMn : clampedCurrentMn + amount;
            return CurrentMn;
        }

        public int ApplyEffectiveMaxMnChange(int oldEffectiveMaxMn, int newEffectiveMaxMn)
        {
            ValidateEffectiveMaximum(oldEffectiveMaxMn, nameof(oldEffectiveMaxMn));
            ValidateEffectiveMaximum(newEffectiveMaxMn, nameof(newEffectiveMaxMn));

            var increase = Math.Max(0, newEffectiveMaxMn - oldEffectiveMaxMn);
            var adjustedMn = Math.Min((long)CurrentMn + increase, newEffectiveMaxMn);
            CurrentMn = (int)Math.Max(0, adjustedMn);
            return CurrentMn;
        }

        internal void ResetAdditionalSkills()
        {
            _skillIds.Clear();
            _skillSlots.Clear();
            _nextSkillSlotId = 1;
            AddSkillSlot(FixedSkillId);
        }

        private void AddSkillSlot(int skillId)
        {
            _skillIds.Add(skillId);
            _skillSlots.Add(new PachimonSkillSlot(_nextSkillSlotId, skillId));
            _nextSkillSlotId++;
        }

        private static void ValidateEffectiveMaxHp(int effectiveMaxHp)
        {
            ValidateEffectiveMaximum(effectiveMaxHp, nameof(effectiveMaxHp));
        }

        private static void ValidateEffectiveMaximum(int effectiveMaximum, string parameterName)
        {
            if (effectiveMaximum < 0) throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public sealed class EquippedItem
    {
        public EquippedItem(
            int itemId,
            string displayName,
            GeneratedItemData generatedData)
        {
            ItemId = itemId;
            DisplayName = displayName;
            GeneratedData = generatedData;
        }

        public int ItemId { get; }
        public string DisplayName { get; }
        public GeneratedItemData GeneratedData { get; }
    }

    public sealed class AppliedEngraving
    {
        public AppliedEngraving(
            int itemId,
            string displayName,
            GeneratedItemData generatedData)
        {
            ItemId = itemId;
            DisplayName = displayName;
            GeneratedData = generatedData;
        }

        public int ItemId { get; }
        public string DisplayName { get; }
        public GeneratedItemData GeneratedData { get; }
    }
}
