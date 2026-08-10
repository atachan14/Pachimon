using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "BurnPursuitPassive",
        menuName = "Pachimon/Passives/Burn Pursuit Passive")]
    public sealed class BurnPursuitPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _damagePercent = 130;

        public int DamagePercent => _damagePercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_damagePercent < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Damage percent cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int damagePercent)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _damagePercent = damagePercent;
        }
#endif
    }
}
