using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Items
{
    [CreateAssetMenu(
        fileName = "DamageItem",
        menuName = "Pachimon/Items/Damage Item")]
    public sealed class DamageItemAsset : ItemAsset
    {
        [SerializeField, Min(1)] private int _damageAmount = 100;

        public int DamageAmount => _damageAmount;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_damageAmount <= 0)
            {
                errors.Add($"{name}: Damage Amount must be positive.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureDamageForEditor(int damageAmount)
        {
            _damageAmount = damageAmount;
        }
#endif
    }
}
