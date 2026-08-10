using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    public abstract class BattleFieldEffectAsset : ScriptableObject
    {
        [SerializeField] private BattleFieldEffectId _effectId;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField, TextArea] private string _description = string.Empty;
        [SerializeField] private Sprite _icon;

        public BattleFieldEffectId EffectId => _effectId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;

        public virtual void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null) return;
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Add($"Field Effect {_effectId}: Display Name is required.");
            }
        }

#if UNITY_EDITOR
        protected void ConfigureDefinitionForEditor(
            BattleFieldEffectId effectId,
            string displayName,
            string description,
            Sprite icon = null)
        {
            _effectId = effectId;
            _displayName = displayName ?? string.Empty;
            _description = description ?? string.Empty;
            _icon = icon;
        }
#endif
    }
}
