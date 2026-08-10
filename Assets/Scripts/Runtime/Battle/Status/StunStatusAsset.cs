using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "StunStatus",
        menuName = "Pachimon/Battle/Status/Stun")]
    public sealed class StunStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Stun)
            {
                errors?.Add("Stun Definition must use Stun ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.Stun,
                displayName,
                description,
                icon);
        }
#endif
    }
}
