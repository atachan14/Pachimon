using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "FireArrowSkill",
        menuName = "Pachimon/Skills/Fire Arrow Skill")]
    public sealed class FireArrowSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _basePower = 100;
        [SerializeField, Min(0)] private int _fireScalingPercent = 100;

        public int BasePower => _basePower;
        public int FireScalingPercent => _fireScalingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Fire)
            {
                errors.Add($"Skill {SkillId}: Fire Arrow must be Fire.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int basePower,
            int fireScalingPercent)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Fire,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _basePower = basePower;
            _fireScalingPercent = fireScalingPercent;
        }
#endif
    }
}
