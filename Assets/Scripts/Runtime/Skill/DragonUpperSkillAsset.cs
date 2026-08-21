using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DragonUpperSkill", menuName = "Pachimon/Skills/Dragon Upper Skill")]
    public sealed class DragonUpperSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseDragonDamage = 100;
        [SerializeField, HideInInspector] private int _dragonDamageRatio = 100;
        [SerializeField, Min(1)] private int _knockoutDurationTicks = 200;
        [SerializeField] private KnockoutStatusAsset _knockoutStatus;

        public int BaseDragonDamage => _baseDragonDamage;
        public int DragonDamageRatio => AttributeDamageRules.ScalingRatio;
        public int KnockoutDurationTicks => _knockoutDurationTicks;
        public KnockoutStatusAsset KnockoutStatus => _knockoutStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Dragon)
                errors?.Add($"Skill {SkillId}: Dragon Upper must be Dragon.");
            if (_knockoutStatus == null)
                errors?.Add($"Skill {SkillId}: Knockout Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName, int baseRecoveryTicks,
            int baseCooldownTicks, int baseManaCost, string description,
            int baseDragonDamage, int dragonDamageRatio,
            int knockoutDurationTicks, KnockoutStatusAsset knockoutStatus)
        {
            base.ConfigureForEditor(
                skillId, displayName, AllocationType.Dragon, true,
                baseRecoveryTicks, baseCooldownTicks, description, baseManaCost);
            _baseDragonDamage = baseDragonDamage;
            _dragonDamageRatio = dragonDamageRatio;
            _knockoutDurationTicks = knockoutDurationTicks;
            _knockoutStatus = knockoutStatus;
        }
#endif
    }
}
