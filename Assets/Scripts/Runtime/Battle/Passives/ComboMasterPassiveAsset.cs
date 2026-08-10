using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "ComboMasterPassive",
        menuName = "Pachimon/Passives/Combo Master Passive")]
    public sealed class ComboMasterPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _damageBonusPerChain = 10;

        public int DamageBonusPerChain => _damageBonusPerChain;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_damageBonusPerChain < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Damage bonus cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int damageBonusPerChain)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _damageBonusPerChain = damageBonusPerChain;
        }
#endif
    }
}
