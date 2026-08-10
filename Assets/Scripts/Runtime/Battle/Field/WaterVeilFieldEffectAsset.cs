using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "WaterVeilFieldEffect",
        menuName = "Pachimon/Battle/Field Effect/Water Veil")]
    public sealed class WaterVeilFieldEffectAsset : BattleFieldEffectAsset
    {
        [SerializeField, Min(0)] private int _healingPerTick = 1;
        [SerializeField, Min(0)] private int _decayPerTick = 1;
        [SerializeField, Range(0, 100)] private int _damageReductionPercent = 30;

        public int HealingPerTick => _healingPerTick;
        public int DecayPerTick => _decayPerTick;
        public int DamageReductionPercent => _damageReductionPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.WaterVeil)
            {
                errors?.Add("Water Veil Definition must use Water Veil ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int healingPerTick,
            int decayPerTick,
            int damageReductionPercent,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.WaterVeil,
                displayName,
                description,
                icon);
            _healingPerTick = healingPerTick;
            _decayPerTick = decayPerTick;
            _damageReductionPercent = damageReductionPercent;
        }
#endif
    }
}
