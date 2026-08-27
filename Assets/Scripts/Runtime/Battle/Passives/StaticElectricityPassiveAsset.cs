using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "StaticElectricityPassive",
        menuName = "Pachimon/Passives/Static Electricity Passive")]
    public sealed class StaticElectricityPassiveAsset : PassiveAsset
    {
        public const int DefaultPassiveId = 36;

        [FormerlySerializedAs("_electricBaseValue")]
        [FormerlySerializedAs("_electricPercent")]
        [SerializeField, Min(0)] private int _baseValue = 25;
        [FormerlySerializedAs("_iceBaseValue")]
        [FormerlySerializedAs("_icePercent")]
        [SerializeField, Min(1)] private int _baseDurationTicks = 25;
        [SerializeField] private SlowStatusAsset _paralysisStatus;

        public int BaseValue => _baseValue;
        public int BaseDurationTicks => _baseDurationTicks;
        public SlowStatusAsset ParalysisStatus => _paralysisStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_baseValue < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Electric Base Value cannot be negative.");
            }

            if (_baseDurationTicks <= 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Base Duration must be positive.");
            }
            if (_paralysisStatus == null)
            {
                errors.Add(
                    $"Passive {PassiveId}: Paralysis Definition is required.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int baseValue,
            int baseDurationTicks,
            SlowStatusAsset paralysisStatus = null)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _baseValue = baseValue;
            _baseDurationTicks = baseDurationTicks;
            _paralysisStatus = paralysisStatus;
        }
#endif
    }
}
