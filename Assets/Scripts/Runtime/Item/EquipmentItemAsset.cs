using System.Collections.Generic;
using Pachimon.Reward;
using UnityEngine;

namespace Pachimon.Items
{
    [CreateAssetMenu(
        fileName = "EquipmentItem",
        menuName = "Pachimon/Items/Equipment Item")]
    public sealed class EquipmentItemAsset : ItemAsset
    {
        [SerializeField] private EquipmentSlot _slot;
        [SerializeField] private PachimonAttribute _mainAttribute;

        public EquipmentSlot Slot => _slot;
        public PachimonAttribute MainAttribute => _mainAttribute;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_slot < EquipmentSlot.Head || _slot > EquipmentSlot.Feet)
            {
                errors.Add($"Item {ItemId}: Equipment Slot is invalid.");
            }
            if (_mainAttribute < PachimonAttribute.Fire
                || _mainAttribute > PachimonAttribute.Dragon)
            {
                errors.Add($"Item {ItemId}: Equipment Attribute is invalid.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureEquipmentForEditor(
            EquipmentSlot slot,
            PachimonAttribute mainAttribute)
        {
            _slot = slot;
            _mainAttribute = mainAttribute;
        }
#endif
    }
}
