using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "CloneStatus", menuName = "Pachimon/Battle/Status/Clone")]
    public sealed class CloneStatusAsset : BattleStatusAsset
    {
        public override string GetDisplayName(BattleStatusInstance instance)
        {
            if (instance == null)
                throw new System.ArgumentNullException(nameof(instance));
            return $"{DisplayName} {instance.StackCount}";
        }

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Clone)
                errors?.Add("Clone Definition must use Clone ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description)
        {
            ConfigureDefinitionForEditor(BattleStatusId.Clone, displayName, description);
        }
#endif
    }
}
