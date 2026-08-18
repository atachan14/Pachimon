using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "PoisonMagicianGrowthStatus",
        menuName = "Pachimon/Battle/Status/Poison Magician Growth")]
    public sealed class PoisonMagicianGrowthStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.PoisonMagicianGrowth)
                errors?.Add("Poison Magician Growth must use its matching ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.PoisonMagicianGrowth,
                displayName,
                description);
        }
#endif
    }
}
