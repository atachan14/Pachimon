using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    public sealed class HealingWindRuntimeData
    {
        public HealingWindRuntimeData(int windBonus, int speedBonus)
        {
            WindBonus = windBonus;
            SpeedBonus = speedBonus;
        }

        public int WindBonus { get; }
        public int SpeedBonus { get; }
    }

    [CreateAssetMenu(fileName = "HealingWindStatus", menuName = "Pachimon/Battle/Status/Healing Wind")]
    public sealed class HealingWindStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.HealingWind)
                errors?.Add("Healing Wind Definition must use HealingWind ID.");
        }
    }
}
