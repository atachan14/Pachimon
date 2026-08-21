using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "CombustionSkill",
        menuName = "Pachimon/Skills/Combustion Skill")]
    public sealed class CombustionSkillAsset : SkillAsset
    {
        [FormerlySerializedAs("_enemyBasePower")]
        [SerializeField, Min(0)] private int _basePower = 100;
        [SerializeField, HideInInspector] private int _fireScalingPercent = 100;

        public int BasePower => _basePower;
        public int FireScalingPercent => AttributeDamageRules.ScalingRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Fire)
            {
                errors.Add($"Skill {SkillId}: Combustion must be Fire.");
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
            int fireScalingPercent,
            bool isMapAssignable = true,
            int baseStartupTicks = 100)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Fire,
                isMapAssignable,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost,
                baseStartupTicks);
            _basePower = basePower;
            _fireScalingPercent = fireScalingPercent;
        }
#endif
    }
}
