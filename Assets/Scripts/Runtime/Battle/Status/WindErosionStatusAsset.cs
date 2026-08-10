using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "WindErosionStatus", menuName = "Pachimon/Battle/Status/Wind Erosion")]
    public sealed class WindErosionStatusAsset : BattleStatusAsset
    {
        [SerializeField, Min(1)] private int _decayPerTick = 1;
        public int DecayPerTick => _decayPerTick;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.WindErosion)
                errors?.Add("Wind Erosion Definition must use WindErosion ID.");
            if (_decayPerTick <= 0)
                errors?.Add("Wind Erosion requires positive Decay Per Tick.");
        }
    }
}
