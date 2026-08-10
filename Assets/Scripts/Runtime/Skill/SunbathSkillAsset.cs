using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "SunbathSkill", menuName = "Pachimon/Skills/Sunbath Skill")]
    public sealed class SunbathSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseHealing = 150;
        [SerializeField, Min(0)] private int _leafHealingRatio = 100;
        [SerializeField, Min(0)] private int _temperatureHealingRatio = 100;
        [SerializeField, Min(0)] private int _rainHealingReductionRatio = 100;

        public int BaseHealing => _baseHealing;
        public int LeafHealingRatio => _leafHealingRatio;
        public int TemperatureHealingRatio => _temperatureHealingRatio;
        public int RainHealingReductionRatio => _rainHealingReductionRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Leaf)
            {
                errors?.Add($"Skill {SkillId}: Sunbath must be Leaf.");
            }
        }
    }
}
