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
        [FormerlySerializedAs("_basePower")]
        [SerializeField, Min(0)] private int _baseDamage = 100;
        [SerializeField, HideInInspector] private int _fireScalingPercent = 100;

        public int BaseDamage => _baseDamage;
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
            int baseDamage,
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
            _baseDamage = baseDamage;
            _fireScalingPercent = fireScalingPercent;
        }
#endif
    }
}
