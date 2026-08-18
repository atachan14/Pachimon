using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "BotanicalGardenPassive", menuName = "Pachimon/Passives/Botanical Garden Passive")]
    public sealed class BotanicalGardenPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _damageBonusPerPlant = 15;
        public int DamageBonusPerPlant => _damageBonusPerPlant;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_damageBonusPerPlant < 0)
                errors.Add($"Passive {PassiveId}: Damage Bonus cannot be negative.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(int passiveId, string displayName,
            string description, int damageBonusPerPlant)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _damageBonusPerPlant = damageBonusPerPlant;
        }
#endif
    }
}
