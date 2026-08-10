using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "LaunchCeremonyStatus",
        menuName = "Pachimon/Battle/Status/Launch Ceremony")]
    public sealed class LaunchCeremonyStatusAsset : BattleStatusAsset
    {
        [SerializeField, Min(0)] private int _aquaMultiplierPercent = 120;
        [SerializeField, Min(0)] private int _manaReductionAquaRatio = 100;

        public int AquaMultiplierPercent => _aquaMultiplierPercent;
        public int ManaReductionAquaRatio => _manaReductionAquaRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.LaunchCeremony)
            {
                errors?.Add("Launch Ceremony Definition must use Launch Ceremony ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int aquaMultiplierPercent,
            int manaReductionAquaRatio,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.LaunchCeremony,
                displayName,
                description,
                icon);
            _aquaMultiplierPercent = aquaMultiplierPercent;
            _manaReductionAquaRatio = manaReductionAquaRatio;
        }
#endif
    }
}
