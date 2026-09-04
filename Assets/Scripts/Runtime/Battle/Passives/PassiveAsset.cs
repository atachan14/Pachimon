using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    public abstract class PassiveAsset : ScriptableObject
    {
        [SerializeField] private int _passiveId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField, Range(1, 3)] private int _minimumPartySize = 1;

        public int PassiveId => _passiveId;

        public string DisplayName => _displayName;

        public virtual string Description => _description;

        public int MinimumPartySize => Mathf.Clamp(_minimumPartySize, 1, 3);

        public virtual void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            if (_passiveId <= 0) errors.Add($"{name}: Passive ID must be positive.");
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Add($"Passive {_passiveId}: display name is missing.");
            }
            // Existing Assets without this newly added field deserialize as 0,
            // which is intentionally treated as the backward-compatible default of 1.
            if (_minimumPartySize < 0 || _minimumPartySize > 3)
            {
                errors.Add($"Passive {_passiveId}: Minimum Party Size must be between 1 and 3.");
            }
        }

#if UNITY_EDITOR
        public void SetDescriptionTemplateForEditor(string descriptionTemplate)
        {
            _description = descriptionTemplate ?? string.Empty;
        }

        public void SetMinimumPartySizeForEditor(int minimumPartySize)
        {
            _minimumPartySize = Mathf.Clamp(minimumPartySize, 1, 3);
        }

        protected void ConfigureBaseForEditor(
            int passiveId,
            string displayName,
            string description)
        {
            _passiveId = passiveId;
            _displayName = displayName;
            _description = description;
        }
#endif
    }
}
