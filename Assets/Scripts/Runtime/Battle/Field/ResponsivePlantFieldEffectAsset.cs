using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "ResponsivePlantFieldEffect", menuName = "Pachimon/Battle/Field Effect/Responsive Plant")]
    public sealed class ResponsivePlantFieldEffectAsset : BattleFieldEffectAsset
    {
        [SerializeField, Min(0)] private int _baseValue = 40;
        [SerializeField, Min(0)] private int _leafRatio = 100;

        public int BaseValue => _baseValue;
        public int LeafRatio => _leafRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.ResponsivePlant)
                errors?.Add("Responsive Plant must use ResponsivePlant ID.");
            if ((Categories & BattleFieldEffectCategory.Plant) == 0)
                errors?.Add("Responsive Plant must use the Plant category.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description,
            int baseValue, int leafRatio)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.ResponsivePlant,
                displayName,
                description,
                categories: BattleFieldEffectCategory.Plant);
            _baseValue = baseValue;
            _leafRatio = leafRatio;
        }
#endif
    }
}
