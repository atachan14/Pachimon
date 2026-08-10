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
        [SerializeField, Min(1)] private int _thresholdNumerator = 30000;
        [SerializeField, Min(1)] private int _thresholdOffset = 200;
        [SerializeField] private FreezeStatusAsset _freezeStatus;

        public int IceValueRatio => _iceValueRatio;
        public int ThresholdNumerator => _thresholdNumerator;
        public int ThresholdOffset => _thresholdOffset;
        public FreezeStatusAsset FreezeStatus => _freezeStatus;

        public int CalculateValue(BattleUnitState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return SignedStatMath.FloorNonNegative(
                source.GetBattleStatValue(PachimonStatType.Ice)
                * _iceValueRatio / 100m);
        }

        public int CalculateFreezeThreshold(int fieldValue)
        {
            if (fieldValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldValue));
            }
            return Math.Max(
                1,
                SignedStatMath.FloorNonNegative(
                    _thresholdNumerator
                    / (decimal)(fieldValue + _thresholdOffset)));
        }

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.FrozenGround)
            {
                errors?.Add("Frozen Ground must use Frozen Ground ID.");
            }
            if (_freezeStatus == null)
            {
                errors?.Add("Frozen Ground requires a Freeze Status.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int iceValueRatio,
            int thresholdNumerator,
            int thresholdOffset,
            FreezeStatusAsset freezeStatus,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.FrozenGround,
                displayName,
                description,
                icon);
            _iceValueRatio = iceValueRatio;
            _thresholdNumerator = thresholdNumerator;
            _thresholdOffset = thresholdOffset;
            _freezeStatus = freezeStatus;
        }
#endif
    }
}
