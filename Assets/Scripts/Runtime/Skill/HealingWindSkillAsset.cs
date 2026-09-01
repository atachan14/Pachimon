using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "HealingWindSkill", menuName = "Pachimon/Skills/Healing Wind Skill")]
    public sealed class HealingWindSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseHealing = 100;

        public int BaseHealing => _baseHealing;
        public int WindRatio => AttributeDamageRules.ScalingRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Wind)
                errors?.Add($"Skill {SkillId}: Healing Wind must be Wind.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName, int baseRecoveryTicks,
            int baseCooldownTicks, int baseManaCost, string description,
            int baseHealing)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Wind,
                true, baseRecoveryTicks, baseCooldownTicks, description,
                baseManaCost);
            _baseHealing = baseHealing;
        }
#endif
    }
}
