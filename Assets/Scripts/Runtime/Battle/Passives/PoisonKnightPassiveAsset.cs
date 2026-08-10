using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "PoisonKnightPassive",
        menuName = "Pachimon/Passives/Poison Knight Passive")]
    public sealed class PoisonKnightPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _baseSharePercent = 30;
        [SerializeField, Min(0)] private int _poisonScalingPercent = 100;

        public int BaseSharePercent => _baseSharePercent;
        public int PoisonScalingPercent => _poisonScalingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_baseSharePercent < 0)
            {
                errors.Add($"Passive {PassiveId}: Base share cannot be negative.");
            }
            if (_poisonScalingPercent < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Poison scaling cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int baseSharePercent,
            int poisonScalingPercent)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _baseSharePercent = baseSharePercent;
            _poisonScalingPercent = poisonScalingPercent;
        }
#endif
    }
}
