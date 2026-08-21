using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "ToxinStatus",
        menuName = "Pachimon/Battle/Status/Toxin")]
    public sealed class ToxinStatusAsset : BattleStatusAsset
    {
        [SerializeField, Min(0)] private int _damagePerTickRatio = 1;
        [SerializeField, Min(0)] private int _decayPerTick = 1;

        public int DamagePerTickRatio => _damagePerTickRatio;
        public int DecayPerTick => _decayPerTick;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Toxin)
            {
                errors?.Add("Toxin Definition must use Toxin ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int damagePerTickRatio,
            int decayPerTick,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.Toxin,
                displayName,
                description,
                icon);
            _damagePerTickRatio = damagePerTickRatio;
            _decayPerTick = decayPerTick;
        }
#endif
    }
}
