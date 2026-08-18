using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "BurningFlowerPassive", menuName = "Pachimon/Passives/Burning Flower Passive")]
    public sealed class BurningFlowerPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _statGainPerDamage = 5;
        [SerializeField] private BurningFlowerGrowthStatusAsset _leafGrowthStatus;
        [SerializeField] private BurningFlowerGrowthStatusAsset _fireGrowthStatus;
        public int StatGainPerDamage => _statGainPerDamage;
        public BurningFlowerGrowthStatusAsset LeafGrowthStatus => _leafGrowthStatus;
        public BurningFlowerGrowthStatusAsset FireGrowthStatus => _fireGrowthStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_statGainPerDamage < 0)
                errors.Add($"Passive {PassiveId}: Stat Gain cannot be negative.");
            if (_leafGrowthStatus == null || _fireGrowthStatus == null)
                errors.Add($"Passive {PassiveId}: Growth Status definitions are required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(int passiveId, string displayName,
            string description, int statGainPerDamage,
            BurningFlowerGrowthStatusAsset leafGrowthStatus,
            BurningFlowerGrowthStatusAsset fireGrowthStatus)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _statGainPerDamage = statGainPerDamage;
            _leafGrowthStatus = leafGrowthStatus;
            _fireGrowthStatus = fireGrowthStatus;
        }
#endif
    }
}
