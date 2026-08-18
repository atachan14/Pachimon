using System;
using System.Collections.Generic;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Items
{
    [CreateAssetMenu(
        fileName = "EngravingItem",
        menuName = "Pachimon/Items/Engraving Item")]
    public sealed class EngravingItemAsset : ItemAsset
    {
        [SerializeField] private PachimonStatType _targetStat;
        [SerializeField, Min(1)] private int _baseEffectValue = 30;

        public PachimonStatType TargetStat => _targetStat;
        public int BaseEffectValue => _baseEffectValue;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_targetStat < 0 || _targetStat >= PachimonStatType.Count)
            {
                errors.Add($"Item {ItemId}: engraving target Stat is invalid.");
            }
            if (_baseEffectValue <= 0)
            {
                errors.Add($"Item {ItemId}: engraving base effect must be positive.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureEngravingForEditor(
            PachimonStatType targetStat,
            int baseEffectValue)
        {
            if (targetStat < 0 || targetStat >= PachimonStatType.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(targetStat));
            }
            if (baseEffectValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseEffectValue));
            }

            _targetStat = targetStat;
            _baseEffectValue = baseEffectValue;
        }
#endif
    }
}
