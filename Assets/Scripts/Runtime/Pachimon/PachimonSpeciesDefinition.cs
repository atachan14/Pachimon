using System;
using UnityEngine;

namespace Pachimon.Data
{
    [Serializable]
    public sealed class PachimonSpeciesDefinition
    {
        public const int MaxDisplayNameLength = 5;

        [SerializeField] private int _speciesId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _frontSprite;
        [SerializeField] private Sprite _backSprite;
        [SerializeField] private AllocationType _allocationType;
        [SerializeField] private int _fixedSkillId;
        [SerializeField] private int _passiveId;
        [SerializeField] private bool _isRunEnabled = true;

        public PachimonSpeciesDefinition(
            int speciesId,
            string displayName,
            Sprite frontSprite,
            Sprite backSprite,
            AllocationType allocationType,
            int fixedSkillId,
            int passiveId,
            bool isRunEnabled = true)
        {
            _speciesId = speciesId;
            _displayName = displayName;
            _frontSprite = frontSprite;
            _backSprite = backSprite;
            _allocationType = allocationType;
            _fixedSkillId = fixedSkillId;
            _passiveId = passiveId;
            _isRunEnabled = isRunEnabled;
        }

        public int SpeciesId => _speciesId;
        public string DisplayName => _displayName;
        public Sprite FrontSprite => _frontSprite;
        public Sprite BackSprite => _backSprite;
        public AllocationType AllocationType => _allocationType;
        public int FixedSkillId => _fixedSkillId;
        public int PassiveId => _passiveId;
        public bool IsRunEnabled => _isRunEnabled;

#if UNITY_EDITOR
        public bool SetGraphicsForEditor(Sprite frontSprite, Sprite backSprite)
        {
            if (_frontSprite == frontSprite && _backSprite == backSprite)
            {
                return false;
            }

            _frontSprite = frontSprite;
            _backSprite = backSprite;
            return true;
        }

        public bool SetDisplayNameForEditor(string displayName)
        {
            displayName = TrimDisplayName(displayName);
            if (_displayName == displayName)
            {
                return false;
            }

            _displayName = displayName;
            return true;
        }

        public bool EnforceDisplayNameLengthForEditor()
        {
            var trimmedName = TrimDisplayName(_displayName);
            if (_displayName == trimmedName)
            {
                return false;
            }

            _displayName = trimmedName;
            return true;
        }

        private static string TrimDisplayName(string displayName)
        {
            return string.IsNullOrEmpty(displayName)
                || displayName.Length <= MaxDisplayNameLength
                    ? displayName
                    : displayName.Substring(0, MaxDisplayNameLength);
        }

        public bool SetPresentationForEditor(
            string displayName,
            Sprite frontSprite,
            Sprite backSprite)
        {
            if (_displayName == displayName
                && _frontSprite == frontSprite
                && _backSprite == backSprite)
            {
                return false;
            }

            _displayName = displayName;
            _frontSprite = frontSprite;
            _backSprite = backSprite;
            return true;
        }

        public bool PopulateMissingLogicIdsForEditor()
        {
            var changed = false;
            if (_fixedSkillId <= 0)
            {
                _fixedSkillId = _speciesId;
                changed = true;
            }

            if (_passiveId <= 0)
            {
                _passiveId = _speciesId;
                changed = true;
            }

            return changed;
        }

        public bool PopulateMissingAllocationTypeForEditor()
        {
            if (_allocationType != AllocationType.Unassigned || _speciesId <= 0)
            {
                return false;
            }

            const int typeCount = 8;
            _allocationType = (AllocationType)(((_speciesId - 1) % typeCount) + 1);
            return true;
        }

        public bool SetRunEnabledForEditor(bool isRunEnabled)
        {
            if (_isRunEnabled == isRunEnabled)
            {
                return false;
            }

            _isRunEnabled = isRunEnabled;
            return true;
        }
#endif
    }
}
