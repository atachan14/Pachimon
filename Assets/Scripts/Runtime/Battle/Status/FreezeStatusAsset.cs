using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "FreezeStatus",
        menuName = "Pachimon/Battle/Status/Freeze")]
    public sealed class FreezeStatusAsset : BattleStatusAsset
    {
        [SerializeField, Min(1)] private int _fireDamagePerDecay = 10;

        public int FireDamagePerDecay => _fireDamagePerDecay;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Freeze)
            {
                errors?.Add("Freeze Definition must use Freeze ID.");
            }
            if (_fireDamagePerDecay <= 0)
            {
                errors?.Add("Freeze Fire Damage Per Decay must be positive.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int fireDamagePerDecay,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.Freeze,
                displayName,
                description,
                icon);
            _fireDamagePerDecay = fireDamagePerDecay;
        }
#endif
    }
}
