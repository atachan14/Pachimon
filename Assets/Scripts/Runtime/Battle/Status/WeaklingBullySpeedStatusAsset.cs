using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "WeaklingBullySpeedStatus",
        menuName = "Pachimon/Battle/Status/Weakling Bully Speed")]
    public sealed class WeaklingBullySpeedStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.WeaklingBullySpeed)
            {
                errors?.Add(
                    "Weakling Bully Speed Definition must use its matching ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description) =>
            ConfigureDefinitionForEditor(
                BattleStatusId.WeaklingBullySpeed,
                displayName,
                description);
#endif
    }
}
