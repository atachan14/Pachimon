using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DragonFootworkSkill", menuName = "Pachimon/Skills/Dragon Footwork Skill")]
    public sealed class DragonFootworkSkillAsset : SkillAsset
    {
        [SerializeField, Min(1)] private int _baseDurationTicks = 80;
        [SerializeField, HideInInspector] private int _durationDragonRatio = 100;
        [SerializeField] private FootworkStatusAsset _footworkStatus;

        public int BaseDurationTicks => _baseDurationTicks;
        public int DurationDragonRatio => AttributeDamageRules.ScalingRatio;
        public FootworkStatusAsset FootworkStatus => _footworkStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Dragon)
                errors?.Add($"Skill {SkillId}: Dragon Footwork must be Dragon.");
            if (_baseDurationTicks <= 0)
                errors?.Add($"Skill {SkillId}: Base Duration must be positive.");
            if (_durationDragonRatio < 0)
                errors?.Add($"Skill {SkillId}: Duration Dragon Ratio cannot be negative.");
            if (_footworkStatus == null)
                errors?.Add($"Skill {SkillId}: Footwork Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int baseDurationTicks,
            int durationDragonRatio,
            FootworkStatusAsset footworkStatus)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Dragon,
                true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _baseDurationTicks = baseDurationTicks;
            _durationDragonRatio = durationDragonRatio;
            _footworkStatus = footworkStatus;
        }
#endif
    }
}
