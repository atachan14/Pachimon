using System.Collections.Generic;
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

        [FormerlySerializedAs("_electricPercent")]
        [SerializeField, Min(0)] private int _electricBaseValue = 20;
        [FormerlySerializedAs("_icePercent")]
        [SerializeField, Min(0)] private int _iceBaseValue = 10;

        public int ElectricBaseValue => _electricBaseValue;
        public int IceBaseValue => _iceBaseValue;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_electricBaseValue < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Electric Base Value cannot be negative.");
            }

            if (_iceBaseValue < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Ice Base Value cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int electricBaseValue,
            int iceBaseValue)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _electricBaseValue = electricBaseValue;
            _iceBaseValue = iceBaseValue;
        }
#endif
    }
}
