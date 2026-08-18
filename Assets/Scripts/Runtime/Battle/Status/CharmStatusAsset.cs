using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "CharmStatus", menuName = "Pachimon/Battle/Status/Charm")]
    public sealed class CharmStatusAsset : BattleStatusAsset
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
            if (StatusId != BattleStatusId.Charm)
                errors?.Add("Charm Definition must use Charm ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.Charm,
                displayName,
                description);
        }
#endif
    }
}
