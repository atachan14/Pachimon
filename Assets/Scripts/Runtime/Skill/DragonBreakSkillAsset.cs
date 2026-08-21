using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DragonBreakSkill", menuName = "Pachimon/Skills/Dragon Break Skill")]
    public sealed class DragonBreakSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseDragonDamage = 100;
        [SerializeField, HideInInspector] private int _dragonDamageRatio = 100;

        public int BaseDragonDamage => _baseDragonDamage;
        public int DragonDamageRatio => AttributeDamageRules.ScalingRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Dragon)
                errors?.Add($"Skill {SkillId}: Dragon Break must be Dragon.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName, int baseRecoveryTicks,
            int baseCooldownTicks, int baseManaCost, string description,
            int baseDragonDamage, int dragonDamageRatio)
        {
            base.ConfigureForEditor(
                skillId, displayName, AllocationType.Dragon, true,
                baseRecoveryTicks, baseCooldownTicks, description, baseManaCost);
            _baseDragonDamage = baseDragonDamage;
            _dragonDamageRatio = dragonDamageRatio;
        }
#endif
    }
}
