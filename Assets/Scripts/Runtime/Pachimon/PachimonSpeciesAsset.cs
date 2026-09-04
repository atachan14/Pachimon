using System.Collections.Generic;
using Pachimon.Passives;
using Pachimon.Run;
using Pachimon.Skills;
using UnityEngine;

namespace Pachimon.Data
{
    [CreateAssetMenu(
        fileName = "PachimonSpecies",
        menuName = "Pachimon/Pachimon Species")]
    public sealed class PachimonSpeciesAsset : ScriptableObject
    {
        public const int MaxDisplayNameLength = 5;

        [Header("Identity")]
        [SerializeField] private int _speciesId;
        [SerializeField] private string _displayName;
        [SerializeField] private AllocationType _allocationType;
        [SerializeField] private bool _isRunEnabled = true;

        [Header("Presentation")]
        [SerializeField] private Sprite _frontSprite;
        [SerializeField] private Sprite _backSprite;

        [Header("Abilities")]
        [SerializeField] private SkillAsset _fixedSkill;
        [SerializeField] private PassiveAsset _passive;

        [Header("Species Initial Stats")]
        [SerializeField] private PachimonInitialStats _initialStats = new();

        public int SpeciesId => _speciesId;
        public string DisplayName => _displayName;
        public AllocationType AllocationType => _allocationType;
        public bool IsRunEnabled => _isRunEnabled;
        public Sprite FrontSprite => _frontSprite;
        public Sprite BackSprite => _backSprite;
        public SkillAsset FixedSkill => _fixedSkill;
        public PassiveAsset Passive => _passive;
        public int FixedSkillId => _fixedSkill != null ? _fixedSkill.SkillId : 0;
        public int PassiveId => _passive != null ? _passive.PassiveId : 0;
        public PachimonInitialStats InitialStats => _initialStats;
        public int MinimumPartySize => Mathf.Max(
            _fixedSkill != null ? _fixedSkill.MinimumPartySize : 1,
            _passive != null ? _passive.MinimumPartySize : 1);

        public void CollectValidationErrors(
            ICollection<string> errors,
            int resourceDisplayMultiplier)
        {
            if (_speciesId <= 0) errors?.Add($"{name}: Species ID must be positive.");
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors?.Add($"Species {_speciesId}: display name is missing.");
            }
            else if (_displayName.Length > MaxDisplayNameLength)
            {
                errors?.Add(
                    $"Species {_speciesId}: display name exceeds "
                    + $"{MaxDisplayNameLength} characters.");
            }

            if (_allocationType == AllocationType.Unassigned)
            {
                errors?.Add($"Species {_speciesId}: Allocation Type is missing.");
            }
            if (_frontSprite == null || _backSprite == null)
            {
                errors?.Add($"Species {_speciesId}: graphic is missing.");
            }
            if (_fixedSkill == null)
            {
                errors?.Add($"Species {_speciesId}: fixed Skill is missing.");
            }
            else if (_fixedSkill.AllocationType != _allocationType)
            {
                errors?.Add(
                    $"Species {_speciesId}: fixed Skill "
                    + $"{_fixedSkill.SkillId} must match {_allocationType}.");
            }
            if (_isRunEnabled && _passive == null)
            {
                errors?.Add(
                    $"Species {_speciesId}: enabled Species requires a Passive.");
            }
            if (_initialStats == null)
            {
                errors?.Add($"Species {_speciesId}: Initial Stats are missing.");
                return;
            }

            foreach (var error in _initialStats.ValidateFixedSubStatBindings())
            {
                errors?.Add($"Species {_speciesId}: {error}");
            }

            foreach (var statType in new[]
                     {
                         PachimonStatType.MaxHp,
                         PachimonStatType.MaxMn,
                     })
            {
                try
                {
                    _initialStats.GetValueUnits(
                        statType,
                        resourceDisplayMultiplier);
                }
                catch (System.InvalidOperationException exception)
                {
                    errors?.Add($"Species {_speciesId}: {exception.Message}");
                }
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int speciesId,
            string displayName,
            Sprite frontSprite,
            Sprite backSprite,
            AllocationType allocationType,
            SkillAsset fixedSkill,
            PassiveAsset passive,
            bool isRunEnabled)
        {
            _speciesId = speciesId;
            _displayName = TrimDisplayName(displayName);
            _frontSprite = frontSprite;
            _backSprite = backSprite;
            _allocationType = allocationType;
            _fixedSkill = fixedSkill;
            _passive = passive;
            _isRunEnabled = isRunEnabled;
            _initialStats ??= new PachimonInitialStats();
        }

        public bool SetGraphicsForEditor(Sprite frontSprite, Sprite backSprite)
        {
            if (_frontSprite == frontSprite && _backSprite == backSprite)
                return false;
            _frontSprite = frontSprite;
            _backSprite = backSprite;
            return true;
        }

        public bool SetDisplayNameForEditor(string displayName)
        {
            displayName = TrimDisplayName(displayName);
            if (_displayName == displayName) return false;
            _displayName = displayName;
            return true;
        }

        public bool EnforceDisplayNameLengthForEditor()
        {
            var trimmed = TrimDisplayName(_displayName);
            if (_displayName == trimmed) return false;
            _displayName = trimmed;
            return true;
        }

        public bool SetPresentationForEditor(
            string displayName,
            Sprite frontSprite,
            Sprite backSprite)
        {
            displayName = TrimDisplayName(displayName);
            if (_displayName == displayName
                && _frontSprite == frontSprite
                && _backSprite == backSprite)
                return false;
            _displayName = displayName;
            _frontSprite = frontSprite;
            _backSprite = backSprite;
            return true;
        }

        public bool PopulateMissingLogicReferencesForEditor(
            SkillAsset fixedSkill,
            PassiveAsset passive)
        {
            var changed = false;
            if (_fixedSkill == null && fixedSkill != null)
            {
                _fixedSkill = fixedSkill;
                changed = true;
            }
            if (_passive == null && passive != null)
            {
                _passive = passive;
                changed = true;
            }
            return changed;
        }

        public bool PopulateMissingAllocationTypeForEditor()
        {
            if (_allocationType != AllocationType.Unassigned || _speciesId <= 0)
                return false;
            _allocationType = (AllocationType)(((_speciesId - 1) % 8) + 1);
            return true;
        }

        public bool SetRunEnabledForEditor(bool isRunEnabled)
        {
            if (_isRunEnabled == isRunEnabled) return false;
            _isRunEnabled = isRunEnabled;
            return true;
        }

        private static string TrimDisplayName(string displayName)
        {
            return string.IsNullOrEmpty(displayName)
                || displayName.Length <= MaxDisplayNameLength
                    ? displayName
                    : displayName.Substring(0, MaxDisplayNameLength);
        }
#endif
    }
}
