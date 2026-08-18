using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "WindRiderPassive", menuName = "Pachimon/Passives/Wind Rider")]
    public sealed class WindRiderPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _speedGainPerHit = 20;
        [SerializeField] private WindRiderGrowthStatusAsset _growthStatus;
        public int SpeedGainPerHit => _speedGainPerHit;
        public WindRiderGrowthStatusAsset GrowthStatus => _growthStatus;
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_growthStatus == null)
                errors?.Add($"Passive {PassiveId}: Growth Status is required.");
        }
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string name, string description,
            int speedGainPerHit, WindRiderGrowthStatusAsset growthStatus)
        {
            ConfigureBaseForEditor(id, name, description);
            _speedGainPerHit = speedGainPerHit;
            _growthStatus = growthStatus;
        }
#endif
    }
}
