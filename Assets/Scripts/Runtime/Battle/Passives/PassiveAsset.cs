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

        public int PassiveId => _passiveId;

        public string DisplayName => _displayName;

        public virtual string Description => _description;

        public virtual void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            if (_passiveId <= 0) errors.Add($"{name}: Passive ID must be positive.");
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Add($"Passive {_passiveId}: display name is missing.");
            }
        }

#if UNITY_EDITOR
        public void SetDescriptionTemplateForEditor(string descriptionTemplate)
        {
            _description = descriptionTemplate ?? string.Empty;
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
