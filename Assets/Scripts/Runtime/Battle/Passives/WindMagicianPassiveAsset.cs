using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "WindMagicianPassive", menuName = "Pachimon/Passives/Wind Magician")]
    public sealed class WindMagicianPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _windGainPerHit = 10;
        [SerializeField] private WindMagicianGrowthStatusAsset _growthStatus;
        public int WindGainPerHit => _windGainPerHit;
        public WindMagicianGrowthStatusAsset GrowthStatus => _growthStatus;
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_growthStatus == null)
                errors?.Add($"Passive {PassiveId}: Growth Status is required.");
        }
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string name, string description,
            int windGainPerHit, WindMagicianGrowthStatusAsset growthStatus)
        {
            ConfigureBaseForEditor(id, name, description);
            _windGainPerHit = windGainPerHit;
            _growthStatus = growthStatus;
        }
#endif
    }
}
