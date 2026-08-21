using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "BackfireSkill",
        menuName = "Pachimon/Skills/Backfire Skill")]
    public sealed class BackfireSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _basePower = 100;
        [SerializeField, HideInInspector] private int _fireScalingPercent = 100;
        [SerializeField, Min(0)] private int _basePenetrationPercent = 10;
        [SerializeField, Min(0)] private int _poisonScalingPercent = 100;

        public int BasePower => _basePower;
        public int FireScalingPercent => AttributeDamageRules.ScalingRatio;
        public int BasePenetrationPercent => _basePenetrationPercent;
        public int PoisonScalingPercent => _poisonScalingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Fire)
            {
                errors.Add($"Skill {SkillId}: Backfire must be Fire.");
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
            int basePenetrationPercent,
            int poisonScalingPercent)
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
            _basePenetrationPercent = basePenetrationPercent;
            _poisonScalingPercent = poisonScalingPercent;
        }
#endif
    }
}
