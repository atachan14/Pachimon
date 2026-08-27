using System;
using System.Collections.Generic;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "FrozenGroundFieldEffect",
        menuName = "Pachimon/Battle/Field Effect/Frozen Ground")]
    public sealed class FrozenGroundFieldEffectAsset : BattleFieldEffectAsset
    {
        [SerializeField, Min(0)] private int _iceValueRatio = 100;
        [SerializeField, Min(1)] private int _durationDoubleValue = 500;

        public int IceValueRatio => _iceValueRatio;
        public int DurationDoubleValue => _durationDoubleValue;

        public int CalculateValue(BattleUnitState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return SignedStatMath.FloorNonNegative(
                source.GetBattleStatValue(PachimonStatType.Ice)
                * _iceValueRatio / 100m);
        }

        public decimal CalculateChillDecayMultiplier(int fieldValue)
        {
            if (fieldValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldValue));
            }
            return 1m / (1m + fieldValue / (decimal)_durationDoubleValue);
        }

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.FrozenGround)
            {
                errors?.Add("Frozen Ground must use Frozen Ground ID.");
            }
            if (_durationDoubleValue <= 0)
            {
                errors?.Add("Frozen Ground Duration Double Value must be positive.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int iceValueRatio,
            int durationDoubleValue,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.FrozenGround,
                displayName,
                description,
                icon);
            _iceValueRatio = iceValueRatio;
            _durationDoubleValue = durationDoubleValue;
        }
#endif
    }
}
