using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Items
{
    public abstract class ItemAsset : ScriptableObject
    {
        [SerializeField] private int _itemId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description;

        public int ItemId => _itemId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;

        public virtual void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            if (_itemId <= 0) errors.Add($"{name}: Item ID must be positive.");
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Add($"Item {_itemId}: display name is missing.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int itemId,
            string displayName,
            Sprite icon,
            string description)
        {
            _itemId = itemId;
            _displayName = displayName;
            _icon = icon;
            _description = description;
        }
#endif
    }
}
