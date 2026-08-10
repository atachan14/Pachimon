using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "DragonBoxerStatus", menuName = "Pachimon/Battle/Status/Dragon Boxer")]
    public sealed class DragonBoxerStatusAsset : BattleStatusAsset
    {
        public override string GetDisplayName(BattleStatusInstance instance)
        {
            if (instance == null)
            {
                throw new System.ArgumentNullException(nameof(instance));
            }
            return $"{DisplayName} {instance.StackCount}";
        }

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.DragonBoxer)
            {
                errors?.Add("Dragon Boxer Definition must use DragonBoxer ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.DragonBoxer,
                displayName,
                description);
        }
#endif
    }
}
