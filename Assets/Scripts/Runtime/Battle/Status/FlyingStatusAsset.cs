using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "FlyingStatus", menuName = "Pachimon/Battle/Status/Flying")]
    public sealed class FlyingStatusAsset : BattleStatusAsset
    {
        [SerializeField, Min(0)] private int _windSpeedRatio = 20;
        public int WindSpeedRatio => _windSpeedRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Flying)
                errors?.Add("Flying Definition must use Flying ID.");
        }
    }
}
