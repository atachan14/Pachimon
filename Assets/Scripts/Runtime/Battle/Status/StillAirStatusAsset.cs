using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "StillAirStatus", menuName = "Pachimon/Battle/Status/Still Air")]
    public sealed class StillAirStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.StillAir)
                errors?.Add("Still Air Definition must use StillAir ID.");
        }
    }
}
