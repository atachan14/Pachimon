using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Items
{
    [CreateAssetMenu(
        fileName = "HealingItem",
        menuName = "Pachimon/Items/Healing Item")]
    public sealed class HealingItemAsset : ItemAsset
    {
        [SerializeField, Min(1)] private int _healAmount = 300;
        [SerializeField] private bool _canRevive;

        public int HealAmount => _healAmount;
        public bool CanRevive => _canRevive;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_healAmount <= 0)
            {
                errors.Add($"{name}: Heal Amount must be positive.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureHealingForEditor(int healAmount, bool canRevive)
        {
            _healAmount = healAmount;
            _canRevive = canRevive;
        }
#endif
    }
}
