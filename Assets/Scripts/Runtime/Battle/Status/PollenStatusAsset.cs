using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "PollenStatus", menuName = "Pachimon/Battle/Status/Pollen")]
    public sealed class PollenStatusAsset : BattleStatusAsset
    {
        [SerializeField, Min(1)] private int _decayPerTick = 1;
        public int DecayPerTick => _decayPerTick;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Pollen)
                errors?.Add("Pollen Definition must use Pollen ID.");
            if (_decayPerTick <= 0)
                errors?.Add("Pollen requires positive Decay Per Tick.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.Pollen,
                displayName,
                description);
            _decayPerTick = 1;
        }
#endif
    }
}
