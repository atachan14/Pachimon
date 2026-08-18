using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "WindGodStatus", menuName = "Pachimon/Battle/Status/Wind God")]
    public sealed class WindGodStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.WindGod)
                errors?.Add("Wind God Definition must use WindGod ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description) =>
            ConfigureDefinitionForEditor(
                BattleStatusId.WindGod,
                displayName,
                description);
#endif
    }
}
