using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "FireArcherPassive",
        menuName = "Pachimon/Passives/Fire Archer Passive")]
    public sealed class FireArcherPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _missingHpPercent = 5;
        [SerializeField, Min(0)] private int _fireScalingPercent = 100;

        public int MissingHpPercent => _missingHpPercent;
        public int FireScalingPercent => _fireScalingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_missingHpPercent < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Missing HP percent cannot be negative.");
            }
            if (_fireScalingPercent < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Fire scaling cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int missingHpPercent,
            int fireScalingPercent)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _missingHpPercent = missingHpPercent;
            _fireScalingPercent = fireScalingPercent;
        }
#endif
    }
}
