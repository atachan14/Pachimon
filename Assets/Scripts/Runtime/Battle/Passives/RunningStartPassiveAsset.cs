using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "RunningStartPassive", menuName = "Pachimon/Passives/Running Start")]
    public sealed class RunningStartPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _startupDamageBonusRatio = 20;
        public int StartupDamageBonusRatio => _startupDamageBonusRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_startupDamageBonusRatio < 0)
                errors?.Add($"Passive {PassiveId}: Startup Damage Bonus Ratio cannot be negative.");
        }
    }
}
