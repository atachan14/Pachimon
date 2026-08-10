using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    public abstract class BattleStatusAsset : ScriptableObject
    {
        [SerializeField] private BattleStatusId _statusId;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField, TextArea] private string _description = string.Empty;
        [SerializeField] private Sprite _icon;

        public BattleStatusId StatusId => _statusId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;

        public virtual string GetDisplayName(BattleStatusInstance instance)
        {
            if (instance == null)
            {
                throw new System.ArgumentNullException(nameof(instance));
            }

            var text = instance.Value > 0
                ? $"{DisplayName} {instance.Value}"
                : DisplayName;
            return instance.RemainingTicks.HasValue
                ? $"{text} [{instance.RemainingTicks.Value}]"
                : text;
        }

        public virtual string GetDescription(BattleStatusInstance instance)
        {
            if (instance == null)
            {
                throw new System.ArgumentNullException(nameof(instance));
            }
            return Description;
        }

        public virtual void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null) return;
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Add($"Status {_statusId}: Display Name is required.");
            }
        }

#if UNITY_EDITOR
        protected void ConfigureDefinitionForEditor(
            BattleStatusId statusId,
            string displayName,
            string description,
            Sprite icon = null)
        {
            _statusId = statusId;
            _displayName = displayName ?? string.Empty;
            _description = description ?? string.Empty;
            _icon = icon;
        }
#endif
    }
}
