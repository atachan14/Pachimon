using System;
using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    public abstract class SkillAsset : ScriptableObject
    {
        [SerializeField] private int _skillId;
        [SerializeField] private string _displayName;
        [SerializeField] private AllocationType _allocationType;
        [SerializeField] private bool _isMapAssignable;
        [SerializeField, Min(0)] private int _baseRecoveryTicks;
        [SerializeField, Min(0)] private int _baseCooldownTicks;
        [SerializeField, Min(0)] private int _baseManaCost;
        [SerializeField, TextArea] private string _description;

        public int SkillId => _skillId;
        public string DisplayName => _displayName;
        public AllocationType AllocationType => _allocationType;
        public bool IsMapAssignable => _isMapAssignable;
        public virtual int BaseStartupTicks => 0;
        public int BaseRecoveryTicks => _baseRecoveryTicks;
        public int BaseCooldownTicks => _baseCooldownTicks;
        public virtual int BaseManaCost => _baseManaCost;
        public virtual bool ConsumesAllCurrentMana => false;
        public virtual string Description => _description;

        public virtual void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));

            if (_skillId <= 0) errors.Add($"{name}: Skill ID must be positive.");
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Add($"Skill {_skillId}: display name is missing.");
            }

            if (_isMapAssignable && _allocationType == AllocationType.Unassigned)
            {
                errors.Add($"Skill {_skillId}: Map-assignable Skill requires an Allocation Type.");
            }

            if (BaseStartupTicks < 0)
            {
                errors.Add($"Skill {_skillId}: Startup cannot be negative.");
            }

            if (_baseRecoveryTicks < 0)
            {
                errors.Add($"Skill {_skillId}: Recovery cannot be negative.");
            }

            if (_baseCooldownTicks < 0)
            {
                errors.Add($"Skill {_skillId}: Cooldown cannot be negative.");
            }

            if (BaseManaCost < 0)
            {
                errors.Add($"Skill {_skillId}: MN Cost cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            AllocationType allocationType,
            bool isMapAssignable,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            string description,
            int baseManaCost = 0)
        {
            _skillId = skillId;
            _displayName = displayName;
            _allocationType = allocationType;
            _isMapAssignable = isMapAssignable;
            _baseRecoveryTicks = baseRecoveryTicks;
            _baseCooldownTicks = baseCooldownTicks;
            _baseManaCost = baseManaCost;
            _description = description;
        }
#endif
    }
}
