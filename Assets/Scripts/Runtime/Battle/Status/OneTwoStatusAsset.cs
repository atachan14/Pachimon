using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "OneTwoStatus", menuName = "Pachimon/Battle/Status/One Two")]
    public sealed class OneTwoStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.OneTwo)
            {
                errors?.Add("One Two Definition must use OneTwo ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.OneTwo,
                displayName,
                description);
        }
#endif
    }
}
