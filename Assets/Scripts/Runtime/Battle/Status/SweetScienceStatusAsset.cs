using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "SweetScienceStatus", menuName = "Pachimon/Battle/Status/Sweet Science")]
    public sealed class SweetScienceStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.SweetScience)
                errors?.Add("Sweet Science Definition must use SweetScience ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description) =>
            ConfigureDefinitionForEditor(
                BattleStatusId.SweetScience,
                displayName,
                description);
#endif
    }
}
