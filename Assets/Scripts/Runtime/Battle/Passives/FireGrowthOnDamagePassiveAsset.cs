using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "FireGrowthOnDamagePassive",
        menuName = "Pachimon/Passives/Fire Growth On Damage Passive")]
    public sealed class FireGrowthOnDamagePassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _fireIncreasePerDamage = 20;

        public int FireIncreasePerDamage => _fireIncreasePerDamage;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_fireIncreasePerDamage < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Fire increase cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int fireIncreasePerDamage)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _fireIncreasePerDamage = fireIncreasePerDamage;
        }
#endif
    }
}
