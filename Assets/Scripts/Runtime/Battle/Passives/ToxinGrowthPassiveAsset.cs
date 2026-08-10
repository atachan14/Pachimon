using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "ToxinGrowthPassive",
        menuName = "Pachimon/Passives/Toxin Growth Passive")]
    public sealed class ToxinGrowthPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _poisonPercentPerApplication = 10;

        public int PoisonPercentPerApplication => _poisonPercentPerApplication;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_poisonPercentPerApplication < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Poison percent cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int poisonPercentPerApplication)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _poisonPercentPerApplication = poisonPercentPerApplication;
        }
#endif
    }
}
