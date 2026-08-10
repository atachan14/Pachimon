using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "FireBarrierFieldEffect",
        menuName = "Pachimon/Field Effect/Fire Barrier")]
    public sealed class FireBarrierFieldEffectAsset : BattleFieldEffectAsset
    {
        [SerializeField, Min(0)] private int _valueHpRatio = 100;
        [SerializeField, Min(0)] private int _valueDurationRatio = 100;
        [SerializeField, Min(0)] private int _valueBurnRatio = 20;
        [SerializeField, Min(0)] private int _defenseSnapshotRatio = 50;
        [SerializeField] private BurnStatusAsset _burnStatus;

        public int ValueHpRatio => _valueHpRatio;
        public int ValueDurationRatio => _valueDurationRatio;
        public int ValueBurnRatio => _valueBurnRatio;
        public int DefenseSnapshotRatio => _defenseSnapshotRatio;
        public BurnStatusAsset BurnStatus => _burnStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.FireBarrier)
            {
                errors?.Add("Fire Barrier Definition must use FireBarrier ID.");
            }
            if (_burnStatus == null)
            {
                errors?.Add("Fire Barrier Definition requires a Burn Status.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int valueHpRatio,
            int valueDurationRatio,
            int valueBurnRatio,
            int defenseSnapshotRatio,
            BurnStatusAsset burnStatus,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.FireBarrier,
                displayName,
                description,
                icon);
            _valueHpRatio = valueHpRatio;
            _valueDurationRatio = valueDurationRatio;
            _valueBurnRatio = valueBurnRatio;
            _defenseSnapshotRatio = defenseSnapshotRatio;
            _burnStatus = burnStatus;
        }
#endif
    }
}
