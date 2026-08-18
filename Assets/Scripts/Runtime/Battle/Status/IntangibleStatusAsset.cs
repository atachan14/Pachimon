using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "IntangibleStatus", menuName = "Pachimon/Battle/Status/Intangible")]
    public sealed class IntangibleStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Intangible)
                errors?.Add("Intangible Definition must use Intangible ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.Intangible,
                displayName,
                description);
        }
#endif
    }
}
