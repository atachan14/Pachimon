using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "FootworkStatus", menuName = "Pachimon/Battle/Status/Footwork")]
    public sealed class FootworkStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Footwork)
                errors?.Add("Footwork Definition must use Footwork ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description) =>
            ConfigureDefinitionForEditor(
                BattleStatusId.Footwork,
                displayName,
                description);
#endif
    }
}
