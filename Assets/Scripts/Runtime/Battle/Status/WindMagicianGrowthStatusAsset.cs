using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "WindMagicianGrowthStatus", menuName = "Pachimon/Battle/Status/Wind Magician Growth")]
    public sealed class WindMagicianGrowthStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.WindMagicianGrowth)
                errors?.Add("Wind Magician Growth must use its matching ID.");
        }
#if UNITY_EDITOR
        public void ConfigureForEditor(string name, string description) =>
            ConfigureDefinitionForEditor(BattleStatusId.WindMagicianGrowth,
                name, description);
#endif
    }
}
