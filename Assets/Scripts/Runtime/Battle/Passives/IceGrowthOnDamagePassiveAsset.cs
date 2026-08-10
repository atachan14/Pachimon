using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "IceGrowthOnDamagePassive",
        menuName = "Pachimon/Passives/Ice Growth On Damage Passive")]
    public sealed class IceGrowthOnDamagePassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _iceIncreasePerDamage = 10;

        public int IceIncreasePerDamage => _iceIncreasePerDamage;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_iceIncreasePerDamage < 0)
            {
                errors?.Add($"Passive {PassiveId}: Ice increase cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int iceIncreasePerDamage)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _iceIncreasePerDamage = iceIncreasePerDamage;
        }
#endif
    }
}
