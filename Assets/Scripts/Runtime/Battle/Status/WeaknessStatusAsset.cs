using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "WeaknessStatus",
        menuName = "Pachimon/Battle/Status/Weakness")]
    public sealed class WeaknessStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Weakness)
                errors?.Add("Weakness Definition must use Weakness ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description) =>
            ConfigureDefinitionForEditor(
                BattleStatusId.Weakness,
                displayName,
                description);
#endif
    }
}
