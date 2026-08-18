using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "DragonInstallStatus", menuName = "Pachimon/Battle/Status/Dragon Install")]
    public sealed class DragonInstallStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.DragonInstall)
                errors?.Add("Dragon Install Definition must use DragonInstall ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description) =>
            ConfigureDefinitionForEditor(
                BattleStatusId.DragonInstall,
                displayName,
                description);
#endif
    }
}
