using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "WaterPulseSkill",
        menuName = "Pachimon/Skills/Water Pulse Skill")]
    public sealed class WaterPulseSkillAsset : SkillAsset
    {
        [SerializeField, HideInInspector] private int _aquaDamageRatio = 100;

        public int AquaDamageRatio => AttributeDamageRules.ScalingRatio;
        public override bool ConsumesAllCurrentMana => true;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Aqua)
            {
                errors?.Add($"Skill {SkillId}: Water Pulse must be Aqua.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            string description,
            int aquaDamageRatio,
            bool isMapAssignable = true,
            int baseStartupTicks = 100)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Aqua,
                isMapAssignable,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost: 0,
                baseStartupTicks);
            _aquaDamageRatio = aquaDamageRatio;
        }
#endif
    }
}
