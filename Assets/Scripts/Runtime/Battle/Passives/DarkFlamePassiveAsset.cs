using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "DarkFlamePassive",
        menuName = "Pachimon/Passives/Dark Flame Passive")]
    public sealed class DarkFlamePassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _baseConversionPercent = 20;
        [SerializeField, Min(0)] private int _poisonScalingPercent = 100;

        public int BaseConversionPercent => _baseConversionPercent;
        public int PoisonScalingPercent => _poisonScalingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_baseConversionPercent < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Base conversion cannot be negative.");
            }
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
            int baseConversionPercent,
            int poisonScalingPercent)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _baseConversionPercent = baseConversionPercent;
            _poisonScalingPercent = poisonScalingPercent;
        }
#endif
    }
}
