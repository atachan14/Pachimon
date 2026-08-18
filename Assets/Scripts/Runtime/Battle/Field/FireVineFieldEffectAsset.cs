using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "FireVineFieldEffect",
        menuName = "Pachimon/Battle/Field Effect/Fire Vine")]
    public sealed class FireVineFieldEffectAsset : BattleFieldEffectAsset
    {
        [SerializeField, Min(0)] private int _baseLeafValue = 15;
        [SerializeField, Min(0)] private int _leafValueRatio = 100;
        [SerializeField, Min(0)] private int _baseFireValue = 15;
        [SerializeField, Min(0)] private int _fireValueRatio = 100;

        public int BaseLeafValue => _baseLeafValue;
        public int LeafValueRatio => _leafValueRatio;
        public int BaseFireValue => _baseFireValue;
        public int FireValueRatio => _fireValueRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.FireVine)
                errors?.Add("Fire Vine Definition must use FireVine ID.");
            if ((Categories & BattleFieldEffectCategory.Plant) == 0)
                errors?.Add("Fire Vine must use the Plant category.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int baseLeafValue,
            int leafValueRatio,
            int baseFireValue,
            int fireValueRatio)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.FireVine,
                displayName,
                description,
                categories: BattleFieldEffectCategory.Plant);
            _baseLeafValue = baseLeafValue;
            _leafValueRatio = leafValueRatio;
            _baseFireValue = baseFireValue;
            _fireValueRatio = fireValueRatio;
        }
#endif
    }
}
