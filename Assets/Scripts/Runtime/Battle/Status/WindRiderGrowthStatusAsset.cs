using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "WindRiderGrowthStatus", menuName = "Pachimon/Battle/Status/Wind Rider Growth")]
    public sealed class WindRiderGrowthStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.WindRiderGrowth)
                errors?.Add("Wind Rider Growth must use its matching ID.");
        }
#if UNITY_EDITOR
        public void ConfigureForEditor(string name, string description) =>
            ConfigureDefinitionForEditor(BattleStatusId.WindRiderGrowth,
                name, description);
#endif
    }
}
