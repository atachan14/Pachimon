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
        [SerializeField, Min(0)] private int _baseTurnCostTicks;
        [SerializeField, Min(0)] private int _baseCooldownTicks;
        [SerializeField, TextArea] private string _description;

        public int SkillId => _skillId;
        public string DisplayName => _displayName;
        public AllocationType AllocationType => _allocationType;
        public bool IsMapAssignable => _isMapAssignable;
        public int BaseTurnCostTicks => _baseTurnCostTicks;
        public int BaseCooldownTicks => _baseCooldownTicks;
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

            if (_baseTurnCostTicks <= 0)
            {
                errors.Add($"Skill {_skillId}: Turn Cost must be positive.");
            }

            if (_baseCooldownTicks < 0)
            {
                errors.Add($"Skill {_skillId}: Cooldown cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            AllocationType allocationType,
            bool isMapAssignable,
            int baseTurnCostTicks,
            int baseCooldownTicks,
            string description)
        {
            _skillId = skillId;
            _displayName = displayName;
            _allocationType = allocationType;
            _isMapAssignable = isMapAssignable;
            _baseTurnCostTicks = baseTurnCostTicks;
            _baseCooldownTicks = baseCooldownTicks;
            _description = description;
        }
#endif
    }
}
