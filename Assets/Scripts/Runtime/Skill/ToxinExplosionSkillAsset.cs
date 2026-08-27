using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ToxinExplosionSkill",
        menuName = "Pachimon/Skills/Toxin Explosion Skill")]
    public sealed class ToxinExplosionSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _toxinConversionPercent = 100;
        [SerializeField, HideInInspector] private int _poisonScalingPercent = 100;
        [SerializeField, Min(0)] private int _aoeFirePercent = 5;
        [SerializeField, HideInInspector] private int _fireScalingPercent = 100;

        public int ToxinConversionPercent => _toxinConversionPercent;
        public int PoisonScalingPercent => AttributeDamageRules.ScalingRatio;
        public int AoeFirePercent => _aoeFirePercent;
        public int FireScalingPercent => AttributeDamageRules.ScalingRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Poison)
            {
                errors.Add($"Skill {SkillId}: Toxin Explosion must be Poison.");
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
            int toxinConversionPercent,
            int poisonScalingPercent,
            int aoeFirePercent,
            int fireScalingPercent)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Poison,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _toxinConversionPercent = toxinConversionPercent;
            _poisonScalingPercent = poisonScalingPercent;
            _aoeFirePercent = aoeFirePercent;
            _fireScalingPercent = fireScalingPercent;
        }
#endif
    }
}
