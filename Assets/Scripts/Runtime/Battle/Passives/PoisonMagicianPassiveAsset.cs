using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "PoisonMagicianPassive", menuName = "Pachimon/Passives/Poison Magician")]
    public sealed class PoisonMagicianPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _poisonGainPerHit = 20;
        [SerializeField] private PoisonMagicianGrowthStatusAsset _growthStatus;

        public int PoisonGainPerHit => _poisonGainPerHit;
        public PoisonMagicianGrowthStatusAsset GrowthStatus => _growthStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_growthStatus == null)
                errors?.Add($"Passive {PassiveId}: Growth Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int poisonGainPerHit,
            PoisonMagicianGrowthStatusAsset growthStatus)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _poisonGainPerHit = poisonGainPerHit;
            _growthStatus = growthStatus;
        }
#endif
    }
}
