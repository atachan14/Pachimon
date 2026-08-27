using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("_recoveryPercent")]
        [SerializeField, Min(1)] private int _recoveryAmount = 500;
        [SerializeField] private bool _canRevive;
        [SerializeField] private bool _defeatedOnly;

        public RecoveryResourceType ResourceType => _resourceType;
        public int RecoveryAmount => _recoveryAmount;
        public bool CanRevive => _canRevive;
        public bool DefeatedOnly => _defeatedOnly;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_recoveryAmount <= 0)
            {
                errors.Add($"{name}: Recovery Amount must be positive.");
            }

            if (_defeatedOnly
                && (_resourceType != RecoveryResourceType.Hp || !_canRevive))
            {
                errors.Add(
                    $"{name}: Defeated-only recovery must restore HP and allow revival.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureHealingForEditor(
            RecoveryResourceType resourceType,
            int recoveryAmount,
            bool canRevive,
            bool defeatedOnly = false)
        {
            _resourceType = resourceType;
            _recoveryAmount = recoveryAmount;
            _canRevive = canRevive;
            _defeatedOnly = defeatedOnly;
        }
#endif
    }
}
