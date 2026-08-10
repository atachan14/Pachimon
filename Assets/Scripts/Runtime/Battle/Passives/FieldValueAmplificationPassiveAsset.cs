using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "FieldValueAmplificationPassive",
        menuName = "Pachimon/Passives/Field Value Amplification Passive")]
    public sealed class FieldValueAmplificationPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _poisonScalingPercent = 30;

        public int PoisonScalingPercent => _poisonScalingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_poisonScalingPercent < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Poison scaling cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int poisonScalingPercent)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _poisonScalingPercent = poisonScalingPercent;
        }
#endif
    }
}
