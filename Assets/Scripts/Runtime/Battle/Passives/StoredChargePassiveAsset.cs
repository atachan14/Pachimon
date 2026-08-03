using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "StoredChargePassive",
        menuName = "Pachimon/Passives/Stored Charge Passive")]
    public sealed class StoredChargePassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _damagePercentPerStack = 10;

        public int DamagePercentPerStack => _damagePercentPerStack;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_damagePercentPerStack <= 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: damage percent per stack must be positive.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int damagePercentPerStack)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _damagePercentPerStack = damagePercentPerStack;
        }
#endif
    }
}
