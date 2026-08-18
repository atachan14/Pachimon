using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Items
{
    public enum RecoveryResourceType
    {
        Hp = 0,
        Mn = 1,
    }

    [CreateAssetMenu(
        fileName = "HealingItem",
        menuName = "Pachimon/Items/Healing Item")]
    public sealed class HealingItemAsset : ItemAsset
    {
        [SerializeField] private RecoveryResourceType _resourceType;
        [SerializeField, Min(1)] private int _recoveryPercent = 50;
        [SerializeField] private bool _canRevive;

        public RecoveryResourceType ResourceType => _resourceType;
        public int RecoveryPercent => _recoveryPercent;
        public bool CanRevive => _canRevive;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_recoveryPercent <= 0)
            {
                errors.Add($"{name}: Recovery Percent must be positive.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureHealingForEditor(
            RecoveryResourceType resourceType,
            int recoveryPercent,
            bool canRevive)
        {
            _resourceType = resourceType;
            _recoveryPercent = recoveryPercent;
            _canRevive = canRevive;
        }
#endif
    }
}
