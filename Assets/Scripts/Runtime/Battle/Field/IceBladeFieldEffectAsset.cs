using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "IceBladeFieldEffect",
        menuName = "Pachimon/Battle/Field Effect/Ice Blade")]
    public sealed class IceBladeFieldEffectAsset : BattleFieldEffectAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.IceBlade)
            {
                errors?.Add("Ice Blade must use Ice Blade ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.IceBlade,
                displayName,
                description,
                icon);
        }
#endif
    }
}
