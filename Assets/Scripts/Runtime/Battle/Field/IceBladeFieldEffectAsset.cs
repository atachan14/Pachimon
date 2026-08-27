using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "IceBladeFieldEffect",
        menuName = "Pachimon/Battle/Field Effect/Ice Blade")]
    public sealed class IceBladeFieldEffectAsset : BattleFieldEffectAsset
    {
        [SerializeField, Min(0)] private int _damagePercent = 25;

        public int DamagePercent => _damagePercent;

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
            int damagePercent = 25,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.IceBlade,
                displayName,
                description,
                icon);
            _damagePercent = damagePercent;
        }
#endif
    }
}
