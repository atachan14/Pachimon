using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    public sealed class ElectricShieldRuntimeData
    {
        public ElectricShieldRuntimeData(
            long shieldApplicationOrder,
            int counterParalysisDurationTicks)
        {
            if (counterParalysisDurationTicks <= 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(counterParalysisDurationTicks));
            }
            ShieldApplicationOrder = shieldApplicationOrder;
            CounterParalysisDurationTicks = counterParalysisDurationTicks;
        }

        public long ShieldApplicationOrder { get; }
        public int CounterParalysisDurationTicks { get; }
    }

    [CreateAssetMenu(fileName = "ElectricShieldStatus", menuName = "Pachimon/Battle/Status/Electric Shield")]
    public sealed class ElectricShieldStatusAsset : BattleStatusAsset
    {
        [SerializeField] private SlowStatusAsset _paralysisStatus;
        public SlowStatusAsset ParalysisStatus => _paralysisStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.ElectricShield)
                errors?.Add("Electric Shield must use ElectricShield ID.");
            if (_paralysisStatus == null)
                errors?.Add("Electric Shield requires Paralysis Status.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            SlowStatusAsset paralysisStatus)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.ElectricShield,
                displayName,
                description);
            _paralysisStatus = paralysisStatus;
        }
#endif
    }
}
