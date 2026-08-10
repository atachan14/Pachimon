using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "TargetSlowDamagePassive",
        menuName = "Pachimon/Passives/Target Slow Damage Passive")]
    public sealed class TargetSlowDamagePassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _slowRatio = 30;

        public int SlowRatio => _slowRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_slowRatio < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Slow Ratio cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int slowRatio)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _slowRatio = slowRatio;
        }
#endif
    }
}
