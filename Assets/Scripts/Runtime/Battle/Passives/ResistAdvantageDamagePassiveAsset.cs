using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "ResistAdvantageDamagePassive",
        menuName = "Pachimon/Passives/Resist Advantage Damage Passive")]
    public sealed class ResistAdvantageDamagePassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _resistDifferenceRatio = 30;

        public int ResistDifferenceRatio => _resistDifferenceRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_resistDifferenceRatio < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Resist difference Ratio cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int resistDifferenceRatio)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _resistDifferenceRatio = resistDifferenceRatio;
        }
#endif
    }
}
